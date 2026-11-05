using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Tumiia.Api.Dtos;

namespace Tumiia.IntegrationTests;

/// <summary>A signed-in user calling the project and document endpoints, and uploading to S3 like a browser would.</summary>
public sealed class VaultClient
{
    private readonly HttpClient _api;
    private readonly HttpClient _s3 = new();

    private VaultClient(HttpClient api)
    {
        _api = api;
    }

    /// <summary>Registers a fresh user and returns a client signed in as them.</summary>
    public static async Task<VaultClient> SignInNewUserAsync(TumiiaApiFactory factory)
    {
        var api = factory.CreateHttpsClient();
        var (session, _, _) = await new AuthClient(api).RegisterNewUserAsync();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return new VaultClient(api);
    }

    public HttpClient Api => _api;

    /// <summary>Creates a project and returns its id.</summary>
    public async Task<Guid> CreateProjectAsync(string name = "Mokoena Residence")
    {
        var response = await _api.PostAsJsonAsync("/api/v1/projects", new { name }, TestContext.Current.CancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>(ApiJson.Options, TestContext.Current.CancellationToken))!.Id;
    }

    /// <summary>Asks for upload addresses for the given files.</summary>
    public Task<HttpResponseMessage> RequestUploadsAsync(Guid projectId, params TestFile[] files)
    {
        var body = new
        {
            captureSource = "drag_drop",
            files = files.Select(file => new { fileName = file.Name, contentType = file.ContentType, sizeBytes = file.Bytes.Length, sha256 = file.Sha256 }),
        };
        return _api.PostAsJsonAsync($"/api/v1/projects/{projectId}/documents/upload-requests", body, TestContext.Current.CancellationToken);
    }

    /// <summary>PUTs bytes to a pre-signed address, as the browser does.</summary>
    public Task<HttpResponseMessage> PutToS3Async(string uploadUrl, byte[] bytes, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return _s3.PutAsync(uploadUrl, content, TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> MarkUploadedAsync(Guid documentId)
    {
        return _api.PostAsync($"/api/v1/documents/{documentId}/uploaded", null, TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(string path)
    {
        return _api.GetAsync(path, TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> GetFromS3Async(string url)
    {
        return _s3.GetAsync(url, TestContext.Current.CancellationToken);
    }

    /// <summary>Fails with the response body in the message, so a test failure shows the API's ProblemDetails.</summary>
    public static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new HttpRequestException($"{(int)response.StatusCode} from {response.RequestMessage?.RequestUri}: {body}");
        }
    }

    /// <summary>Runs the whole upload for one file (request, PUT, uploaded) and returns the document id.</summary>
    public async Task<Guid> UploadAsync(Guid projectId, TestFile file)
    {
        var response = await RequestUploadsAsync(projectId, file);
        await EnsureSuccessAsync(response);
        var target = (await response.Content.ReadFromJsonAsync<UploadRequestsResponse>(ApiJson.Options, TestContext.Current.CancellationToken))!.Files.Single();
        await EnsureSuccessAsync(await PutToS3Async(target.UploadUrl!, file.Bytes, file.ContentType));
        await EnsureSuccessAsync(await MarkUploadedAsync(target.DocumentId!.Value));
        return target.DocumentId.Value;
    }
}

/// <summary>A synthetic file: random bytes with a name, type and SHA-256.</summary>
public sealed record TestFile(string Name, string ContentType, byte[] Bytes)
{
    public string Sha256 => Convert.ToHexStringLower(SHA256.HashData(Bytes));

    /// <summary>A random file of the given size, so every test uploads different content.</summary>
    public static TestFile Random(string name, string contentType = "application/pdf", int size = 2048)
    {
        return new TestFile(name, contentType, RandomNumberGenerator.GetBytes(size));
    }
}
