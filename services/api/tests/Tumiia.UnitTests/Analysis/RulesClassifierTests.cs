using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class RulesClassifierTests
{
    [Fact]
    public void Score_WhenTheTextIsAQuote_RanksTheQuoteCategoryFirst()
    {
        var scores = RulesClassifier.Score(SampleDocuments.Quote, "scan.pdf", SampleDocuments.Taxonomy);

        scores.MaxBy(score => score.Value).Key.ShouldBe("commercial_quote");
        scores.Values.Sum().ShouldBe(1, 0.0001);
    }

    [Fact]
    public void Score_WhenOnlyTheFileNameSaysWhatItIs_StillHasAnOpinion()
    {
        var scores = RulesClassifier.Score(string.Empty, "Contractor_Quote-v2.pdf", SampleDocuments.Taxonomy);

        scores.MaxBy(score => score.Value).Key.ShouldBe("commercial_quote");
    }

    [Fact]
    public void Score_WhenNothingMatches_HasNoOpinion()
    {
        RulesClassifier.Score("zzz qqq", "IMG_3837.jpg", SampleDocuments.Taxonomy).ShouldBeEmpty();
    }

    [Fact]
    public void Score_NeverProposesNeedsReview()
    {
        var scores = RulesClassifier.Score(SampleDocuments.Payment, "pop.pdf", SampleDocuments.Taxonomy);

        scores.ShouldNotContainKey(TaxonomyCategory.NeedsReviewKey);
        scores.MaxBy(score => score.Value).Key.ShouldBe("payment");
    }

    [Fact]
    public void Score_WhenAConfiguredPatternIsBroken_IgnoresItInsteadOfFailing()
    {
        var broken = new TaxonomyCategory { Key = "broken", DisplayName = "Broken", Patterns = ["(unclosed"], Keywords = ["brick"] };

        var scores = RulesClassifier.Score("one brick", "a.txt", [broken]);

        scores["broken"].ShouldBe(1);
    }

    [Theory]
    [InlineData("the quote is here", "quote", 1)]
    [InlineData("quotes and quotation", "quote", 0)]
    [InlineData("proof of payment, proof of payment", "proof of payment", 2)]
    [InlineData("anything", "", 0)]
    public void CountWord_CountsOnlyWholeWordsAndPhrases(string text, string word, int expected)
    {
        RulesClassifier.CountWord(text, word).ShouldBe(expected);
    }
}
