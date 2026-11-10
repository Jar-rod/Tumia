using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;

namespace Tumiia.UnitTests.Infrastructure.Analysis;

/// <summary>Stands in for the Bedrock endpoint: records each request body and answers with a canned response.</summary>
public sealed class FakeBedrock : HttpMessageHandler
{
    public List<JsonNode> Requests { get; } = [];

    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = Message("{}");

    /// <summary>When set, sending throws this instead of answering (as the AWS signer does without credentials).</summary>
    public Exception? Failure { get; set; }

    /// <summary>A real Anthropic client pointed at this fake, with retries off so failures are quick.</summary>
    public AnthropicClient Client() => new() { ApiKey = "test", BaseUrl = "https://bedrock.test", HttpClient = new HttpClient(this), MaxRetries = 0 };

    /// <summary>A Messages API response whose only text block is <paramref name="text"/>.</summary>
    public static string Message(string text, string stopReason = "end_turn") => JsonSerializer.Serialize(new
    {
        id = "msg_test",
        type = "message",
        role = "assistant",
        model = "anthropic.claude-haiku-5-5",
        content = new[] { new { type = "text", text } },
        stop_reason = stopReason,
        stop_sequence = (string?)null,
        usage = new { input_tokens = 100, output_tokens = 50 },
    });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Requests.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!);
        return new HttpResponseMessage(Status) { Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json") };
    }
}
