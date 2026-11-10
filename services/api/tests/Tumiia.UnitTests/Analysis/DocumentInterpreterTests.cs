using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Analysis;

public class DocumentInterpreterTests
{
    private static Interpretation Interpret(DocumentText text, HostedAnalysis? hosted = null, string fileName = "scan.pdf", FileFormat? format = null, IReadOnlyList<TaxonomyCategory>? taxonomy = null)
    {
        return DocumentInterpreter.Interpret(new InterpretationRequest(text, format ?? SampleDocuments.Pdf(), fileName, taxonomy ?? SampleDocuments.Taxonomy, hosted, new ConfidenceWeights()));
    }

    private static HostedField Field(string name, string value, double confidence = 0.95, string snippet = "") => new(name, value, confidence, 1, snippet);

    private static readonly HostedAnalysis QuoteAnswer = SampleDocuments.Hosted(
        "commercial_quote",
        fields:
        [
            Field("party", "ABC Supplies (Pty) Ltd", snippet: "ABC Supplies (Pty) Ltd"),
            Field("amount", "18450.00", snippet: "Total R 18,450.00"),
            Field("currency", "ZAR", snippet: "Total R 18,450.00"),
            Field("document_date", "2026-10-12", snippet: "Date: 12/10/2026"),
            Field("subject", "Bricks", 0.8, "Bricks NFX"),
        ]);

    [Fact]
    public void Interpret_WhenTheAiIsSureAboutALowRiskDocument_FilesItAutomatically()
    {
        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Quote), QuoteAnswer);

        result.Category.Key.ShouldBe("commercial_quote");
        result.Confidence.Band.ShouldBe(ConfidenceBand.High);
        result.Outcome.ShouldBe(DocumentStatus.AutoFiled);
        result.Summary.ShouldBe("Quote from ABC Supplies (Pty) Ltd for Bricks — R18,450.00, 12 Oct 2026.");
        result.Confidence.Signals.Agreement.ShouldBe(1);
    }

    [Fact]
    public void Interpret_UsesTheAisFactsWithTheirEvidenceAndPutsTheDocumentTypeFirst()
    {
        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Quote), QuoteAnswer);

        result.Fields[0].Name.ShouldBe(FieldNames.DocumentType);
        result.Fields[0].Method.ShouldBe(FieldMethods.Hosted);
        var amount = result.Fields.Single(field => field.Name == FieldNames.Amount);
        amount.Method.ShouldBe(FieldMethods.Hosted);
        amount.Evidence.ShouldBe(new FieldEvidence("bedrock", 1, null, "Total R 18,450.00"));
        result.Fields.ShouldContain(field => field.Name == FieldNames.Language && field.Value == "en");
    }

    [Fact]
    public void Interpret_WhenTheAiIsSureAboutAPayment_StillWaitsForAPerson()
    {
        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Payment), SampleDocuments.Hosted("payment", 0.99, fields: [Field("amount", "2000.00"), Field("document_date", "2026-08-14"), Field("party", "Jean Habimana")]));

        result.Confidence.Band.ShouldBe(ConfidenceBand.High);
        result.Outcome.ShouldBe(DocumentStatus.AwaitingConfirmation);
    }

    [Fact]
    public void Interpret_WhenTheAiIsUnsure_SendsItToNeedsReview()
    {
        var unsure = SampleDocuments.Hosted("commercial_quote", 0.3, "invoice_receipt", 0.25, readability: 0.3);

        Interpret(new DocumentText(), unsure, "IMG_3837.jpg", new FileFormat(DetectedFormat.Jpeg, "image/jpeg", true)).Outcome.ShouldBe(DocumentStatus.NeedsReview);
    }

    [Fact]
    public void Interpret_WhenTheAiCouldHardlyReadAPhotoFromACamera_IsAtMostFairlySure()
    {
        var text = new DocumentText { Image = new ImageFacts(DateTimeOffset.UtcNow, null, null, "Apple") };

        var result = Interpret(text, SampleDocuments.Hosted("progress_evidence", 0.99, readability: 0.1), "IMG_1.jpg", new FileFormat(DetectedFormat.Jpeg, "image/jpeg", true));

        result.Confidence.Band.ShouldBe(ConfidenceBand.Medium);
        result.Fields.ShouldContain(field => field.Name == FieldNames.DocumentDate && field.Method == FieldMethods.Exif);
    }

    [Fact]
    public void Interpret_WhenTheKeywordsPointElsewhere_LowersTheConfidence()
    {
        var agreeing = Interpret(SampleDocuments.Typed(SampleDocuments.Quote), QuoteAnswer);
        var disagreeing = Interpret(SampleDocuments.Typed(SampleDocuments.Quote), QuoteAnswer with { CategoryKey = "invoice_receipt" });

        disagreeing.Confidence.Signals.Agreement.ShouldBe(0);
        disagreeing.Confidence.Score.ShouldBeLessThan(agreeing.Confidence.Score);
    }

    [Fact]
    public void Interpret_WhenTheAiSaysNeedsReview_SendsItToNeedsReview()
    {
        var result = Interpret(new DocumentText(), SampleDocuments.Hosted(TaxonomyCategory.NeedsReviewKey, 0.9));

        result.Outcome.ShouldBe(DocumentStatus.NeedsReview);
        result.Summary.ShouldBe("I'm not sure what this is.");
    }

    [Fact]
    public void Interpret_KeepsTheAisWarningsAndWarnsAboutNonEnglishText()
    {
        var answer = SampleDocuments.Hosted("invoice_receipt") with { Language = "fr", Warnings = ["The total is cut off."] };

        var result = Interpret(new DocumentText(), answer);

        result.Language.ShouldBe("fr");
        result.Warnings.ShouldContain("The total is cut off.");
        result.Warnings.ShouldContain(warning => warning.Contains("not in English"));
    }

    [Fact]
    public void Interpret_WhenTheAiNamesACategoryThatDoesNotExist_FallsBackToTheKeywords()
    {
        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Quote), SampleDocuments.Hosted("made_up"));

        result.Category.Key.ShouldBe("commercial_quote");
        result.Fields[0].Method.ShouldBe(FieldMethods.Keyword);
    }

    [Fact]
    public void Interpret_WhenTheAiWasNotUsed_ClassifiesByKeywordsButSendsItToNeedsReview()
    {
        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Quote));

        result.Category.Key.ShouldBe("commercial_quote");
        result.Outcome.ShouldBe(DocumentStatus.NeedsReview);
        result.Warnings.ShouldContain(warning => warning.Contains("keywords only"));
        result.Fields.Single(field => field.Name == FieldNames.Amount).Value.ShouldBe("18450.00");
    }

    [Fact]
    public void Interpret_WhenNothingIsRecognised_SendsItToNeedsReviewWithoutGuessing()
    {
        var result = Interpret(new DocumentText { Warnings = ["The file could not be read."] }, fileName: "IMG_3837.png", format: new FileFormat(DetectedFormat.Png, "image/png", true));

        result.Category.Key.ShouldBe(TaxonomyCategory.NeedsReviewKey);
        result.Summary.ShouldBe("I'm not sure what this is.");
        result.Warnings.ShouldContain("The file could not be read.");
        result.Fields.ShouldHaveSingleItem().Name.ShouldBe(FieldNames.DocumentType);
    }

    [Fact]
    public void Interpret_WhenOneFieldIsUncertain_StillFilesAClearlyTypedDocument()
    {
        var answer = QuoteAnswer with { Fields = [.. QuoteAnswer.Fields.Where(field => field.Name != "document_date"), Field("document_date", "2026-04-03", 0.4)] };

        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Quote), answer);

        result.Fields.Single(field => field.Name == FieldNames.DocumentDate).Confidence.ShouldBe(0.4);
        result.Outcome.ShouldBe(DocumentStatus.AutoFiled);
    }

    [Fact]
    public void Interpret_WhenADwgHasOnlyItsHeader_StaysBelowTheDwgCap()
    {
        var text = new DocumentText { FileHeaderFacts = ["AutoCAD DWG drawing, version 2018"] };

        var result = Interpret(text, SampleDocuments.Hosted("design", 0.99), "Ground floor plan.dwg", new FileFormat(DetectedFormat.Dwg, "image/vnd.dwg", true));

        result.Category.Key.ShouldBe("design");
        result.Confidence.Score.ShouldBeLessThanOrEqualTo(ConfidenceCalculator.DwgCap);
    }

    [Fact]
    public void Interpret_WhenACategoryIsAddedThroughConfigurationOnly_ClassifiesAndExtractsItByKeywords()
    {
        var payslip = new TaxonomyCategory
        {
            Key = "payslip",
            DisplayName = "Payslip",
            Consequence = ConsequenceLevel.Medium,
            Keywords = ["payslip", "UIF", "gross pay", "CTC"],
            FieldsToSave =
            [
                new FieldDefinition { Name = "gross_pay", Type = FieldType.Money, Labels = ["gross pay"] },
                new FieldDefinition { Name = "pay_date", Type = FieldType.Date, Labels = ["pay date"] },
            ],
        };

        var result = Interpret(SampleDocuments.Typed(SampleDocuments.Payslip), fileName: "october.pdf", taxonomy: [.. SampleDocuments.Taxonomy, payslip]);

        result.Category.Key.ShouldBe("payslip");
        result.Fields.Single(field => field.Name == "gross_pay").Value.ShouldBe("ZAR 25000.00");
        result.Fields.Single(field => field.Name == "pay_date").Value.ShouldBe("2026-10-25");
    }
}
