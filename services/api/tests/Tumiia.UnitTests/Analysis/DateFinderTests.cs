using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class DateFinderTests
{
    [Theory]
    [InlineData("Issued 2026-10-12", "2026-10-12")]
    [InlineData("Date: 25/10/2026", "2026-10-25")]
    [InlineData("12.10.26", "2026-10-12")]
    [InlineData("12 Oct 2026", "2026-10-12")]
    [InlineData("1st September 2026", "2026-09-01")]
    [InlineData("Oct 12, 2026", "2026-10-12")]
    public void FindAll_WhenTheTextHasADate_ReadsItAsAnIsoDate(string text, string expected)
    {
        var date = DateFinder.FindAll(text).ShouldHaveSingleItem();

        DateFinder.Format(date.Date).ShouldBe(expected);
    }

    [Fact]
    public void FindAll_WhenBothNumbersCouldBeTheMonth_ReadsDayFirstAndSaysItIsAmbiguous()
    {
        var date = DateFinder.FindAll("03/04/2026").ShouldHaveSingleItem();

        date.Date.ShouldBe(new DateOnly(2026, 4, 3));
        date.Ambiguous.ShouldBeTrue();
    }

    [Theory]
    [InlineData("31/02/2026")]
    [InlineData("12/10/1890")]
    [InlineData("Item 12-13-14")]
    public void FindAll_WhenTheDateCannotExist_SkipsIt(string text)
    {
        DateFinder.FindAll(text).ShouldBeEmpty();
    }

    [Fact]
    public void FindAll_WhenThereAreSeveralDates_ReturnsThemInTheOrderTheyAppear()
    {
        var dates = DateFinder.FindAll("From 1 Nov 2026 to 2026-11-30");

        dates.Select(date => date.Date).ShouldBe([new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)]);
    }
}
