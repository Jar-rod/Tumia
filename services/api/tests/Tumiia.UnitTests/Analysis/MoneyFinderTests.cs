using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class MoneyFinderTests
{
    [Theory]
    [InlineData("Total R18,450.00", 18450.00, "ZAR")]
    [InlineData("ZAR 2 000", 2000, "ZAR")]
    [InlineData("Montant: €2.000,50", 2000.50, "EUR")]
    [InlineData("Paid 150,000 RWF to the mason", 150000, "RWF")]
    [InlineData("KSh 3,500 for transport", 3500, "KES")]
    [InlineData("US$ 1,200.99", 1200.99, "USD")]
    [InlineData("£75", 75, "GBP")]
    [InlineData("ZiG 400.50", 400.50, "ZWG")]
    public void FindWithCurrency_WhenALineShowsAnAmountWithACurrency_ReadsBoth(string line, decimal amount, string currency)
    {
        var found = MoneyFinder.FindWithCurrency(line).ShouldHaveSingleItem();

        found.Amount.ShouldBe(amount);
        found.Currency.ShouldBe(currency);
    }

    [Theory]
    [InlineData("PLEASE CALL FOR 2026 PRICES")]
    [InlineData("Quantity 20000 bricks")]
    public void FindWithCurrency_WhenThereIsNoCurrency_FindsNothing(string line)
    {
        MoneyFinder.FindWithCurrency(line).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("18,450.00", 18450.00)]
    [InlineData("18 450,00", 18450.00)]
    [InlineData("18.450,00", 18450.00)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("2000", 2000)]
    [InlineData("12,5", 12.5)]
    public void TryParseNumber_ReadsTheDecimalMarkFromWhatFollowsTheLastSeparator(string text, decimal expected)
    {
        MoneyFinder.TryParseNumber(text, out var amount).ShouldBeTrue();
        amount.ShouldBe(expected);
    }

    [Fact]
    public void TryParseNumber_WhenTheNumberIsZero_SaysNo()
    {
        MoneyFinder.TryParseNumber("0.00", out _).ShouldBeFalse();
    }

    [Fact]
    public void FindFirst_WhenNoCurrencyIsShown_ReturnsTheBareNumber()
    {
        var found = MoneyFinder.FindFirst("25 000.00 before deductions").ShouldNotBeNull();

        found.Amount.ShouldBe(25000.00m);
        found.Currency.ShouldBeNull();
    }

    [Fact]
    public void FindFirst_WhenThereIsNoNumber_ReturnsNull()
    {
        MoneyFinder.FindFirst("not stated").ShouldBeNull();
    }

    [Fact]
    public void Format_WritesTwoDecimalsWithAPoint()
    {
        MoneyFinder.Format(18450m).ShouldBe("18450.00");
    }
}
