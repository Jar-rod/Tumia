using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class CategoryChooserTests
{
    [Fact]
    public void Choose_PicksTheMostLikelyCategoryAndTheRunnerUp()
    {
        var choice = CategoryChooser.Choose(new Dictionary<string, double> { ["invoice"] = 0.3, ["quote"] = 0.7 });

        choice.WinnerKey.ShouldBe("quote");
        choice.WinnerProbability.ShouldBe(0.7);
        choice.RunnerUpKey.ShouldBe("invoice");
        choice.RunnerUpProbability.ShouldBe(0.3);
        choice.HasOpinion.ShouldBeTrue();
    }

    [Fact]
    public void Choose_WhenOnlyOneCategoryMatched_HasNoRunnerUp()
    {
        var choice = CategoryChooser.Choose(new Dictionary<string, double> { ["payment"] = 1 });

        choice.RunnerUpKey.ShouldBeNull();
        choice.RunnerUpProbability.ShouldBe(0);
    }

    [Fact]
    public void Choose_WhenNothingMatched_ProposesNeedsReview()
    {
        var choice = CategoryChooser.Choose(new Dictionary<string, double>());

        choice.WinnerKey.ShouldBe(TaxonomyCategory.NeedsReviewKey);
        choice.HasOpinion.ShouldBeFalse();
    }
}
