using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tumiia.Api.Dtos;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Documents;
using Tumiia.Infrastructure.Aws;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.IntegrationTests;

[Collection(ApiCollection.Name)]
public class UploadFlowTests(TumiiaApiFactory factory)
{
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, TestContext.Current.CancellationToken))!;
    }

    /// <summary>Waits for the analysis message for one document (other tests share the queue).</summary>
    private async Task<string?> ReceiveQueueMessageForAsync(Guid documentId)
    {
        using var sqs = AwsClients.CreateSqs(factory.AwsSettings);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var received = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest { QueueUrl = factory.QueueUrl, MaxNumberOfMessages = 10, WaitTimeSeconds = 1, VisibilityTimeout = 0 }, TestContext.Current.CancellationToken);
            var match = received.Messages?.FirstOrDefault(message => message.Body.Contains(documentId.ToString()));
            if (match is not null)
            {
                await sqs.DeleteMessageAsync(factory.QueueUrl, match.ReceiptHandle, TestContext.Current.CancellationToken);
                return match.Body;
            }
        }

        return null;
    }

    private async Task<List<AuditEventType>> AuditEventsForAsync(Guid documentId)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>();
        return await database.AuditEvents.Where(auditEvent => auditEvent.DocumentId == documentId)
            .Select(auditEvent => auditEvent.EventType)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Upload_RequestThenPutThenUploaded_StoresTheFileAndQueuesItForAnalysis()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();
        var file = TestFile.Random("Quote ABC Supplies.pdf");

        var request = await ReadAsync<UploadRequestsResponse>(await user.RequestUploadsAsync(projectId, file));
        var target = request.Files.ShouldHaveSingleItem();
        var put = await user.PutToS3Async(target.UploadUrl!, file.Bytes, file.ContentType);
        var uploaded = await user.MarkUploadedAsync(target.DocumentId!.Value);
        var message = await ReceiveQueueMessageForAsync(target.DocumentId.Value);

        put.StatusCode.ShouldBe(HttpStatusCode.OK);
        uploaded.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<DocumentResponse>(uploaded)).Status.ShouldBe(DocumentStatus.New);
        message.ShouldNotBeNull();
        JsonDocument.Parse(message).RootElement.GetProperty("documentId").GetGuid().ShouldBe(target.DocumentId.Value);
        (await AuditEventsForAsync(target.DocumentId.Value)).ShouldBe([AuditEventType.DocumentUploadRequested, AuditEventType.DocumentUploaded], ignoreOrder: true);
    }

    [Fact]
    public async Task Upload_WhenTheSameFileIsSentAgain_ReturnsTheExistingDocument()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();
        var file = TestFile.Random("invoice.pdf");
        var firstId = await user.UploadAsync(projectId, file);

        var again = await ReadAsync<UploadRequestsResponse>(await user.RequestUploadsAsync(projectId, file with { Name = "invoice (1).pdf" }));

        var target = again.Files.ShouldHaveSingleItem();
        target.ExistingDocumentId.ShouldBe(firstId);
        target.UploadUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Upload_WhenThePutIsADifferentSize_IsRefusedByStorageAndTheDocumentStaysWaiting()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();
        var file = TestFile.Random("plan.pdf");
        var target = (await ReadAsync<UploadRequestsResponse>(await user.RequestUploadsAsync(projectId, file))).Files.Single();

        var put = await user.PutToS3Async(target.UploadUrl!, file.Bytes[..1000], file.ContentType);
        var uploaded = await user.MarkUploadedAsync(target.DocumentId!.Value);

        put.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        uploaded.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UploadRequest_WhenAFileTypeIsNotAllowed_IsRejectedWithAClearMessage()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();

        var response = await user.RequestUploadsAsync(projectId, TestFile.Random("setup.exe", "application/octet-stream"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        problem.Errors["files[0]"].Single().ShouldStartWith("setup.exe: TUMIIA can't take .exe files.");
    }

    [Fact]
    public async Task UploadRequest_WhenOverFiftyFiles_IsRejected()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();
        var files = Enumerable.Range(0, 51).Select(index => TestFile.Random($"file-{index}.pdf", size: 16)).ToArray();

        var response = await user.RequestUploadsAsync(projectId, files);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NonMember_GetsNotFoundForTheProjectAndItsDocuments()
    {
        var owner = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await owner.CreateProjectAsync();
        var documentId = await owner.UploadAsync(projectId, TestFile.Random("contract.pdf"));
        var stranger = await VaultClient.SignInNewUserAsync(factory);

        HttpResponseMessage[] responses =
        [
            await stranger.GetAsync($"/api/v1/projects/{projectId}"),
            await stranger.GetAsync($"/api/v1/projects/{projectId}/documents"),
            await stranger.RequestUploadsAsync(projectId, TestFile.Random("x.pdf")),
            await stranger.GetAsync($"/api/v1/documents/{documentId}"),
            await stranger.GetAsync($"/api/v1/documents/{documentId}/source-url"),
            await stranger.MarkUploadedAsync(documentId),
        ];

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SourceUrl_ForAnUploadedPdf_OpensTheOriginalInTheBrowser()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();
        var file = TestFile.Random("Quote ABC.pdf");
        var documentId = await user.UploadAsync(projectId, file);

        var source = await ReadAsync<SourceUrlResponse>(await user.GetAsync($"/api/v1/documents/{documentId}/source-url"));
        var download = await user.GetFromS3Async(source.Url);

        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBe(file.Bytes);
        download.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("inline");
        download.Content.Headers.ContentDisposition.FileName!.Trim('"').ShouldBe("Quote ABC.pdf");
    }

    [Fact]
    public async Task Documents_AreListedAndFilteredByStatusAndCategory()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await user.CreateProjectAsync();
        var documentId = await user.UploadAsync(projectId, TestFile.Random("site photo.jpg", "image/jpeg"));

        var all = await ReadAsync<List<DocumentResponse>>(await user.GetAsync($"/api/v1/projects/{projectId}/documents"));
        var newOnes = await ReadAsync<List<DocumentResponse>>(await user.GetAsync($"/api/v1/projects/{projectId}/documents?status=New"));
        var failed = await ReadAsync<List<DocumentResponse>>(await user.GetAsync($"/api/v1/projects/{projectId}/documents?status=Failed"));
        var quotes = await ReadAsync<List<DocumentResponse>>(await user.GetAsync($"/api/v1/projects/{projectId}/documents?category=commercial_quote"));

        all.ShouldHaveSingleItem().Id.ShouldBe(documentId);
        all[0].CaptureSource.ShouldBe(CaptureSource.DragDrop);
        newOnes.ShouldHaveSingleItem();
        failed.ShouldBeEmpty();
        quotes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Projects_AreCreatedListedAndReadByTheirOwner()
    {
        var user = await VaultClient.SignInNewUserAsync(factory);

        var created = await user.Api.PostAsJsonAsync("/api/v1/projects", new { name = "  Erf 4521 Midrand " }, TestContext.Current.CancellationToken);
        var project = await ReadAsync<ProjectResponse>(created);
        var list = await ReadAsync<List<ProjectResponse>>(await user.GetAsync("/api/v1/projects"));

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.Headers.Location!.ToString().ShouldBe($"/api/v1/projects/{project.Id}");
        project.Name.ShouldBe("Erf 4521 Midrand");
        list.ShouldHaveSingleItem().Id.ShouldBe(project.Id);
    }
}
