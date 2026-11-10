using System.Text;
using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;
using Tumiia.Infrastructure.Aws;
using Tumiia.Infrastructure.Persistence;
using Tumiia.Infrastructure.Persistence.Configurations;
using Tumiia.Infrastructure.Storage;
using Tumiia.UnitTests.Analysis;

namespace Tumiia.UnitTests.Infrastructure.Analysis;

public class TaxonomyAndStorageTests
{
    [Fact]
    public void Seed_HasTheElevenV11CategoriesEndingWithNeedsReview()
    {
        var keys = SampleDocuments.Taxonomy.Select(category => category.Key).ToList();

        keys.ShouldBe(["project_site", "design", "commercial_quote", "contract_appointment", "invoice_receipt", "payment", "approval_legal", "programme_task", "progress_evidence", "communication", "needs_review"]);
    }

    [Fact]
    public void Seed_MarksPaymentContractAndLegalAsHighConsequence()
    {
        var high = SampleDocuments.Taxonomy.Where(category => category.Consequence == ConsequenceLevel.High).Select(category => category.Key);

        high.ShouldBe(["contract_appointment", "payment", "approval_legal"], ignoreOrder: true);
    }

    [Fact]
    public void Seed_EveryCategoryHasADescriptionValidThresholdsAndPatternsThatCompile()
    {
        foreach (var category in SampleDocuments.Taxonomy)
        {
            category.Description.ShouldNotBeNullOrWhiteSpace();
            category.HighThreshold.ShouldBeGreaterThan(category.MediumThreshold);
            foreach (var pattern in category.Patterns)
            {
                Should.NotThrow(() => new Regex(pattern), $"{category.Key}: {pattern}");
            }
        }
    }

    [Fact]
    public void Parse_ReadsATaxonomyFileWithEnumsAsNames()
    {
        const string json = """{ "categories": [ { "key": "payslip", "displayName": "Payslip", "consequence": "Medium", "fieldsToSave": [ { "name": "gross_pay", "type": "Money", "labels": ["gross pay"] } ] } ] }""";

        var category = TaxonomySeed.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))).ShouldHaveSingleItem();

        category.Consequence.ShouldBe(ConsequenceLevel.Medium);
        category.FieldsToSave.ShouldHaveSingleItem().Type.ShouldBe(FieldType.Money);
        category.HighThreshold.ShouldBe(0.75);
    }

    [Fact]
    public void AnalysisJson_RoundTripsExtractedFields()
    {
        var fields = new List<ExtractedField> { new("amount", "18450.00", 0.9, new FieldEvidence("text", 1, 10, "Total R 18,450.00"), FieldMethods.Regex) };

        var json = AnalysisJson.Write(fields);

        json.ShouldContain("\"page\":1");
        AnalysisJson.Read<List<ExtractedField>>(json).ShouldBe(fields);
        Should.Throw<InvalidOperationException>(() => AnalysisJson.Read<List<ExtractedField>>("null"));
    }

    [Fact]
    public void CaptureSource_Paste_IsStoredAsPaste()
    {
        DocumentConfiguration.ToText(CaptureSource.Paste).ShouldBe("paste");
        DocumentConfiguration.FromText("paste").ShouldBe(CaptureSource.Paste);
    }

    [Fact]
    public async Task S3FileStore_DownloadsAndUploadsWholeFiles()
    {
        var s3 = Substitute.For<IAmazonS3>();
        s3.GetObjectAsync("bucket", "originals/p/d", Arg.Any<CancellationToken>()).Returns(new GetObjectResponse { ResponseStream = new MemoryStream([1, 2, 3]) });
        var store = new S3FileStore(s3, Options.Create(new StorageSettings { DocumentsBucket = "bucket" }), Options.Create(new AwsSettings()), TimeProvider.System);

        (await store.DownloadAsync("originals/p/d", CancellationToken.None)).ShouldBe([1, 2, 3]);
        await store.UploadAsync("originals/p/e", [4, 5], "text/plain", CancellationToken.None);

        await s3.Received().PutObjectAsync(Arg.Is<PutObjectRequest>(request => request.BucketName == "bucket" && request.Key == "originals/p/e" && request.ContentType == "text/plain"), Arg.Any<CancellationToken>());
    }
}
