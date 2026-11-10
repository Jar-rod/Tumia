using System.Text.Json;
using Anthropic.Models.Messages;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Bedrock;

/// <summary>Reads the model's JSON answer into a <see cref="HostedAnalysis"/>, or says why it can't be used.</summary>
public static class BedrockResponseReader
{
    /// <summary>The analysis, or null and the reason.</summary>
    public static (HostedAnalysis? Analysis, string? Problem) Read(Message message, string model)
    {
        if (message.StopReason == StopReason.Refusal)
        {
            return (null, "The AI declined to read this document.");
        }

        if (message.StopReason == StopReason.MaxTokens)
        {
            return (null, "The AI's answer was cut off.");
        }

        var json = string.Concat(message.Content.Select(block => block.Value).OfType<TextBlock>().Select(text => text.Text));
        return string.IsNullOrWhiteSpace(json) ? (null, "The AI gave no answer.") : Parse(json, model);
    }

    /// <summary>Parses the answer JSON. Missing or wrong-typed values are treated as absent, never as errors.</summary>
    public static (HostedAnalysis? Analysis, string? Problem) Parse(string json, string model)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var category = Text(root, "category");
            if (string.IsNullOrWhiteSpace(category))
            {
                return (null, "The AI's answer had no category.");
            }

            var runnerUp = Text(root, "runnerUpCategory");
            var analysis = new HostedAnalysis(
                category,
                Number(root, "categoryConfidence"),
                runnerUp is null or BedrockPrompt.NoRunnerUp ? null : runnerUp,
                Number(root, "runnerUpConfidence"),
                Number(root, "readability"),
                Text(root, "language") ?? LanguageDetector.Undetermined,
                Fields(root),
                Strings(root, "warnings"),
                model);
            return (analysis, null);
        }
        catch (JsonException)
        {
            return (null, "The AI's answer could not be read.");
        }
    }

    private static List<HostedField> Fields(JsonElement root)
    {
        if (!root.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return fields.EnumerateArray()
            .Where(field => field.ValueKind == JsonValueKind.Object)
            .Select(field => new HostedField(Text(field, "name") ?? string.Empty, Text(field, "value") ?? string.Empty, Number(field, "confidence"), (int)Number(field, "page"), Text(field, "snippet") ?? string.Empty))
            .ToList();
    }

    private static List<string> Strings(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).Where(item => item.Length > 0).ToList()
            : [];
    }

    private static string? Text(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static double Number(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0;
    }
}
