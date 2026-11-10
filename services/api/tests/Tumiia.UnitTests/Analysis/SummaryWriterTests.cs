using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class SummaryWriterTests
{
    private static ExtractedField Field(string name, string value) => new(name, value, 0.9, new FieldEvidence("text"), FieldMethods.Regex);

    [Fact]
    public void Write_WhenEveryFactWasFound_WritesOnePlainLine()
    {
        var fields = new[]
        {
            Field(FieldNames.Party, "ABC Supplies"),
            Field(FieldNames.Subject, "Bricks"),
            Field(FieldNames.Amount, "18450.00"),
            Field(FieldNames.Currency, "ZAR"),
            Field(FieldNames.DocumentDate, "2026-10-12"),
        };

        SummaryWriter.Write("Quote", fields).ShouldBe("Quote from ABC Supplies for Bricks — R18,450.00, 12 Oct 2026.");
    }

    [Fact]
    public void Write_WhenOnlySomeFactsWereFound_MentionsOnlyThose()
    {
        SummaryWriter.Write("Progress / Evidence", [Field(FieldNames.DocumentDate, "2026-08-03")]).ShouldBe("Progress / Evidence — 3 Aug 2026.");
        SummaryWriter.Write("Communication", []).ShouldBe("Communication.");
    }

    [Fact]
    public void Write_WhenTheCurrencyHasNoSymbol_UsesItsCode()
    {
        var fields = new[] { Field(FieldNames.Amount, "150000.00"), Field(FieldNames.Currency, "XOF") };

        SummaryWriter.Write("Invoice / Receipt", fields).ShouldBe("Invoice / Receipt — XOF 150,000.00.");
    }
}
