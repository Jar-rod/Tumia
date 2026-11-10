using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class HostedFieldReaderTests
{
    private static HostedField Field(string name, string value, double confidence = 0.9, string snippet = "", int page = 1) => new(name, value, confidence, page, snippet);

    [Theory]
    [InlineData("amount", "R 18,450.00", "18450.00")]
    [InlineData("amount", "ZAR 2 000", "2000.00")]
    [InlineData("currency", "zar", "ZAR")]
    [InlineData("document_date", "12 Oct 2026", "2026-10-12")]
    [InlineData("party", "  ABC Supplies  ", "ABC Supplies")]
    public void Read_RewritesValuesInTumiiasFormats(string name, string value, string expected)
    {
        var (fields, warnings) = HostedFieldReader.Read([Field(name, value)], new DocumentText());

        fields.ShouldHaveSingleItem().Value.ShouldBe(expected);
        warnings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("amount", "about half")]
    [InlineData("currency", "rand")]
    [InlineData("document_date", "last week")]
    public void Read_WhenAValueCannotBeRead_LeavesItOutAndSaysSo(string name, string value)
    {
        var (fields, warnings) = HostedFieldReader.Read([Field(name, value)], new DocumentText());

        fields.ShouldBeEmpty();
        warnings.ShouldHaveSingleItem().ShouldContain("left out");
    }

    [Fact]
    public void Read_WhenTheQuoteIsInTheTypedText_KeepsTheConfidence()
    {
        var text = SampleDocuments.Typed("Total:   R 18,450.00\nthanks");

        var field = HostedFieldReader.Read([Field("amount", "18450.00", 0.9, "total: R 18,450.00")], text).Fields.Single();

        field.Confidence.ShouldBe(0.9);
        field.Evidence.ShouldBe(new FieldEvidence("bedrock", 1, null, "total: R 18,450.00"));
        field.Method.ShouldBe(FieldMethods.Hosted);
    }

    [Fact]
    public void Read_WhenTheQuoteIsNotInTheTypedText_HalvesTheConfidenceAndNotesIt()
    {
        var field = HostedFieldReader.Read([Field("party", "Acme Ltd", 0.8, "Acme Ltd")], SampleDocuments.Typed("Invoice from Mokoena Hardware")).Fields.Single();

        field.Confidence.ShouldBe(0.4, 0.0001);
        field.Evidence.Snippet!.ShouldEndWith("(not found in the document's text)");
    }

    [Fact]
    public void Read_WhenThereIsNoTypedText_CannotCheckTheQuoteAndTrustsIt()
    {
        var field = HostedFieldReader.Read([Field("party", "Acme Ltd", 0.8, "Acme Ltd", page: 0)], new DocumentText()).Fields.Single();

        field.Confidence.ShouldBe(0.8);
        field.Evidence.Page.ShouldBeNull();
    }

    [Fact]
    public void Read_SkipsBlankFactsRepeatsAndTheDocumentTypeAndClampsConfidence()
    {
        var (fields, _) = HostedFieldReader.Read(
            [Field("party", "A", 1.7), Field("party", "B"), Field("", "x"), Field("subject", " "), Field("document_type", "Quote")],
            new DocumentText());

        fields.ShouldHaveSingleItem().Value.ShouldBe("A");
        fields[0].Confidence.ShouldBe(1);
    }
}
