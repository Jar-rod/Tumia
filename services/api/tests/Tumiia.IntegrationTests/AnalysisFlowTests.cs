using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tumiia.Api.Dtos;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;
using Tumiia.Infrastructure.Persistence;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Tumiia.IntegrationTests;

/// <summary>
/// Capture, read and interpret against the real database, S3 and SQS (LocalStack): paste or upload a synthetic file,
/// run the analysis the worker would run, and read the result back through the API. Bedrock is off (the default),
/// so the keyword rules classify; the Bedrock adapter itself is covered by unit tests against a fake endpoint.
/// </summary>
[Collection(ApiCollection.Name)]
public class AnalysisFlowTests(TumiiaApiFactory factory)
{
    private const string PastedQuote = """
        ABC Supplies (Pty) Ltd
        QUOTATION No: Q-2001
        Date: 12/10/2026
        Site: Erf 1234, Midrand
        Bricks NFX 20000
        Total R 18,450.00
        Valid for 30 days
        """;

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await VaultClient.EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, TestContext.Current.CancellationToken))!;
    }

    private Task<HttpResponseMessage> PasteAsync(VaultClient client, Guid projectId, string text, string? title = null)
    {
        return client.Api.PostAsJsonAsync($"/api/v1/projects/{projectId}/documents/text", new { text, title }, TestContext.Current.CancellationToken);
    }

    private async Task<AnalyseDocumentOutcome> AnalyseAsync(Guid documentId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AnalyseDocument>().ExecuteAsync(documentId, TestContext.Current.CancellationToken);
    }

    private static byte[] QuotePdf()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        var lines = PastedQuote.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        for (var index = 0; index < lines.Length; index++)
        {
            page.AddText(lines[index] + " " + Guid.NewGuid().ToString("N")[..6], 12, new PdfPoint(40, 780 - (index * 20)), font);
        }

        return builder.Build();
    }

    [Fact]
    public async Task PastedText_IsStoredUnchangedQueuedAndAnalysedIntoAProposal()
    {
        var client = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await client.CreateProjectAsync();
        var text = PastedQuote + "\nRef " + Guid.NewGuid();

        var pasted = await ReadAsync<DocumentResponse>(await PasteAsync(client, projectId, text, "Quote from WhatsApp"));

        pasted.CaptureSource.ShouldBe(CaptureSource.Paste);
        pasted.Status.ShouldBe(DocumentStatus.New);
        pasted.OriginalFilename.ShouldBe("Quote from WhatsApp.txt");

        (await AnalyseAsync(pasted.Id)).ShouldBe(AnalyseDocumentOutcome.Analysed);
        var detail = await ReadAsync<DocumentDetailResponse>(await client.GetAsync($"/api/v1/documents/{pasted.Id}"));

        var analysis = detail.Analysis.ShouldNotBeNull();
        analysis.ProposedCategory.ShouldBe("commercial_quote");
        analysis.ProposedCategoryName.ShouldBe("Quote");
        analysis.Fields[0].Name.ShouldBe(FieldNames.DocumentType);
        analysis.Fields.ShouldContain(field => field.Name == FieldNames.Amount && field.Value == "18450.00");
        analysis.Fields.ShouldContain(field => field.Name == FieldNames.Currency && field.Value == "ZAR");
        analysis.Fields.Single(field => field.Name == FieldNames.Amount).Evidence.Snippet.ShouldBe("Total R 18,450.00");
        analysis.Summary.ShouldStartWith("Quote from ABC Supplies (Pty) Ltd");
        detail.Document.Status.ShouldBe(analysis.Outcome);
        detail.ContentTypeDetected.ShouldBe("text/plain");

        var source = await ReadAsync<SourceUrlResponse>(await client.GetAsync($"/api/v1/documents/{pasted.Id}/source-url"));
        var original = await (await client.GetFromS3Async(source.Url)).Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        original.ShouldBe(Encoding.UTF8.GetBytes(text));
    }

    [Fact]
    public async Task PastingTheSameTextTwice_ReturnsTheFirstDocument()
    {
        var client = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await client.CreateProjectAsync();
        var text = "Please deliver the blocks on Monday. " + Guid.NewGuid();

        var first = await PasteAsync(client, projectId, text);
        var second = await PasteAsync(client, projectId, text);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<DocumentResponse>(second)).Id.ShouldBe((await ReadAsync<DocumentResponse>(first)).Id);
    }

    [Fact]
    public async Task PastingNothing_IsRefusedWithAPlainMessage()
    {
        var client = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await client.CreateProjectAsync();

        var response = await PasteAsync(client, projectId, "   ");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("Paste some text first.");
    }

    [Fact]
    public async Task PastingIntoSomeoneElsesProject_AnswersNotFound()
    {
        var owner = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await owner.CreateProjectAsync();
        var outsider = await VaultClient.SignInNewUserAsync(factory);

        (await PasteAsync(outsider, projectId, "Hello")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadedPdf_IsReadFromItsTextLayerAndKeptUnchanged()
    {
        var client = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await client.CreateProjectAsync();
        var file = new TestFile("Contractor_Quote.pdf", "application/pdf", QuotePdf());
        var documentId = await client.UploadAsync(projectId, file);

        await AnalyseAsync(documentId);
        var detail = await ReadAsync<DocumentDetailResponse>(await client.GetAsync($"/api/v1/documents/{documentId}"));

        detail.Analysis!.ProposedCategory.ShouldBe("commercial_quote");
        detail.ContentTypeDetected.ShouldBe("application/pdf");
        detail.ExtensionMatches.ShouldBe(true);
        detail.Document.ConsequenceLevel.ShouldBe(ConsequenceLevel.Medium);
        // Bedrock is off in tests, so the keyword rules decide and a person reviews it.
        detail.Document.Status.ShouldBe(DocumentStatus.NeedsReview);
        detail.Analysis.Warnings.ShouldContain(warning => warning.Contains("keywords only"));
        var source = await ReadAsync<SourceUrlResponse>(await client.GetAsync($"/api/v1/documents/{documentId}/source-url"));
        var stored = await (await client.GetFromS3Async(source.Url)).Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        SHA256.HashData(stored).ShouldBe(SHA256.HashData(file.Bytes));
    }

    [Fact]
    public async Task AnalysingTheSameDocumentTwice_KeepsOneAnalysis()
    {
        var client = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await client.CreateProjectAsync();
        var pasted = await ReadAsync<DocumentResponse>(await PasteAsync(client, projectId, "Site instruction: pour the slab on Friday. " + Guid.NewGuid()));

        (await AnalyseAsync(pasted.Id)).ShouldBe(AnalyseDocumentOutcome.Analysed);
        (await AnalyseAsync(pasted.Id)).ShouldBe(AnalyseDocumentOutcome.AlreadyAnalysed);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>();
        (await database.DocumentAnalyses.CountAsync(analysis => analysis.DocumentId == pasted.Id, TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task UnreadableFile_GoesToNeedsReviewWithAWarning()
    {
        var client = await VaultClient.SignInNewUserAsync(factory);
        var projectId = await client.CreateProjectAsync();
        var documentId = await client.UploadAsync(projectId, TestFile.Random("IMG_3837.pdf", "application/pdf"));

        await AnalyseAsync(documentId);
        var detail = await ReadAsync<DocumentDetailResponse>(await client.GetAsync($"/api/v1/documents/{documentId}"));

        detail.Document.Status.ShouldBe(DocumentStatus.NeedsReview);
        detail.Document.NeedsReview.ShouldBeTrue();
        detail.ExtensionMatches.ShouldBe(false);
        detail.Analysis!.Warnings.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Taxonomy_IsSeededIntoTheDatabaseOnFirstUse()
    {
        using var scope = factory.Services.CreateScope();
        var categories = await scope.ServiceProvider.GetRequiredService<ITaxonomyStore>().ListAsync(TestContext.Current.CancellationToken);

        categories.Count.ShouldBe(11);
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>();
        (await database.TaxonomyCategories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(11);
    }
}
