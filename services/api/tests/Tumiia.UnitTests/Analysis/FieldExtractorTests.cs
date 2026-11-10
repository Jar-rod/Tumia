using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class FieldExtractorTests
{
    private static IReadOnlyList<ExtractedField> Extract(string text, string categoryKey = "commercial_quote", double lineConfidence = 1.0)
    {
        return FieldExtractor.Extract(SampleDocuments.Typed(text, lineConfidence), SampleDocuments.Category(categoryKey), SampleDocuments.Taxonomy, "en");
    }

    [Fact]
    public void Extract_WhenAQuoteIsReadable_FindsPartyDateAmountCurrencySubjectAndSite()
    {
        var fields = Extract(SampleDocuments.Quote);

        SampleDocuments.Field(fields, FieldNames.Party)!.Value.ShouldBe("ABC Supplies (Pty) Ltd");
        SampleDocuments.Field(fields, FieldNames.DocumentDate)!.Value.ShouldBe("2026-10-12");
        SampleDocuments.Field(fields, FieldNames.Amount)!.Value.ShouldBe("18450.00");
        SampleDocuments.Field(fields, FieldNames.Currency)!.Value.ShouldBe("ZAR");
        SampleDocuments.Field(fields, FieldNames.Subject)!.Value.ShouldBe("Bricks");
        SampleDocuments.Field(fields, FieldNames.SiteReference)!.Value.ShouldBe("Erf 1234");
        SampleDocuments.Field(fields, FieldNames.WorkPackage)!.Value.ShouldBe("Foundation brickwork");
        SampleDocuments.Field(fields, FieldNames.Language)!.Value.ShouldBe("en");
    }

    [Fact]
    public void Extract_PrefersTheAmountOnTheTotalLineAndPointsToThatLine()
    {
        var amount = SampleDocuments.Field(Extract(SampleDocuments.Quote), FieldNames.Amount)!;

        amount.Confidence.ShouldBe(0.9, 0.0001);
        amount.Evidence.Page.ShouldBe(1);
        amount.Evidence.Line.ShouldBe(10);
        amount.Evidence.Snippet.ShouldBe("Total R 18,450.00");
        amount.Method.ShouldBe(FieldMethods.Regex);
    }

    [Fact]
    public void Extract_WhenNoLineIsLabelledTotal_TakesTheLargestAmountWithLowerConfidence()
    {
        var amount = SampleDocuments.Field(Extract("Bricks R 1 000\nCement R 3 000"), FieldNames.Amount)!;

        amount.Value.ShouldBe("3000.00");
        amount.Confidence.ShouldBe(0.6, 0.0001);
    }

    [Fact]
    public void Extract_WhenTheDateCouldBeReadEitherWay_SaysSoInTheEvidenceAndLowersConfidence()
    {
        var date = SampleDocuments.Field(Extract("Date: 03/04/2026"), FieldNames.DocumentDate)!;

        date.Value.ShouldBe("2026-04-03");
        date.Evidence.Snippet!.ShouldEndWith("(read as day-first)");
        date.Confidence.ShouldBeLessThan(0.9);
    }

    [Fact]
    public void Extract_ScalesEachFieldByHowWellItsLineWasRead()
    {
        var amount = SampleDocuments.Field(Extract("Total R 500.00", lineConfidence: 0.5), FieldNames.Amount)!;

        amount.Confidence.ShouldBe(0.45, 0.0001);
    }

    [Fact]
    public void Extract_WhenAPaymentNamesTheRecipient_TakesItAsTheParty()
    {
        var fields = Extract(SampleDocuments.Payment, "payment");

        SampleDocuments.Field(fields, FieldNames.Party)!.Value.ShouldBe("Jean Habimana");
        SampleDocuments.Field(fields, FieldNames.Amount)!.Value.ShouldBe("2000.00");
        SampleDocuments.Field(fields, FieldNames.Currency)!.Value.ShouldBe("EUR");
        SampleDocuments.Field(fields, FieldNames.DocumentDate)!.Value.ShouldBe("2026-08-14");
    }

    [Fact]
    public void Extract_WhenASentenceOnlyMentionsFrom_DoesNotReadItAsALabel()
    {
        var fields = Extract("Got a quote from the guy for bricks");

        SampleDocuments.Field(fields, FieldNames.Party).ShouldBeNull();
    }

    [Fact]
    public void Extract_WhenNothingIsInTheText_InventsNothing()
    {
        FieldExtractor.Extract(new DocumentText(), SampleDocuments.Category("commercial_quote"), SampleDocuments.Taxonomy, LanguageDetector.Undetermined).ShouldBeEmpty();
    }

    [Fact]
    public void Extract_WhenAPhotoHasExifData_TakesItsDateAndPosition()
    {
        var text = new DocumentText { Image = new ImageFacts(new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero), -1.9441, 30.0619, "Apple") };

        var fields = FieldExtractor.Extract(text, SampleDocuments.Category("progress_evidence"), SampleDocuments.Taxonomy, LanguageDetector.Undetermined);

        var date = SampleDocuments.Field(fields, FieldNames.DocumentDate)!;
        date.Value.ShouldBe("2026-08-03");
        date.Method.ShouldBe(FieldMethods.Exif);
        SampleDocuments.Field(fields, FieldNames.Location)!.Value.ShouldBe("-1.944100, 30.061900");
    }

    [Fact]
    public void Extract_WhenTheProposedCategoryHasNoMatchingChip_LooksAtEveryCategorysChipsWithLowerConfidence()
    {
        var subject = SampleDocuments.Field(Extract("The sheets arrived. Roofing done.", "communication"), FieldNames.Subject)!;

        subject.Value.ShouldBe("Roofing");
        subject.Confidence.ShouldBe(0.5, 0.0001);
    }

    [Fact]
    public void Extract_WhenACategoryDefinesItsOwnFields_FindsThemByTheirLabels()
    {
        var payslip = new TaxonomyCategory
        {
            Key = "payslip",
            DisplayName = "Payslip",
            FieldsToSave =
            [
                new FieldDefinition { Name = "gross_pay", Type = FieldType.Money, Labels = ["gross pay"] },
                new FieldDefinition { Name = "uif", Type = FieldType.Money, Labels = ["UIF"] },
                new FieldDefinition { Name = "pay_date", Type = FieldType.Date, Labels = ["pay date"] },
                new FieldDefinition { Name = "employee_number", Type = FieldType.Number, Labels = ["employee no"] },
                new FieldDefinition { Name = "missing", Type = FieldType.Text, Labels = ["not there"] },
            ],
        };

        var fields = FieldExtractor.Extract(SampleDocuments.Typed(SampleDocuments.Payslip + "\nEmployee no 4471"), payslip, [payslip], "en");

        SampleDocuments.Field(fields, "gross_pay")!.Value.ShouldBe("ZAR 25000.00");
        SampleDocuments.Field(fields, "uif")!.Value.ShouldBe("ZAR 177.12");
        SampleDocuments.Field(fields, "pay_date")!.Value.ShouldBe("2026-10-25");
        SampleDocuments.Field(fields, "employee_number")!.Value.ShouldBe("4471");
        SampleDocuments.Field(fields, "missing").ShouldBeNull();
    }

    [Theory]
    [InlineData(FieldType.Money, "about 2 000", "2000.00")]
    [InlineData(FieldType.Date, "no date here", null)]
    [InlineData(FieldType.Number, "none", null)]
    [InlineData(FieldType.Text, "  Mokoena  ", "  Mokoena  ")]
    public void ReadAs_ReadsAValueAsItsType(FieldType type, string raw, string? expected)
    {
        FieldExtractor.ReadAs(type, raw).ShouldBe(expected);
    }
}
