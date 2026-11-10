using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Analysis;

public class FilingDeciderTests
{
    [Theory]
    [InlineData("progress_evidence", ConfidenceBand.High, DocumentStatus.AutoFiled)]
    [InlineData("commercial_quote", ConfidenceBand.High, DocumentStatus.AutoFiled)]
    [InlineData("commercial_quote", ConfidenceBand.Medium, DocumentStatus.AwaitingConfirmation)]
    [InlineData("payment", ConfidenceBand.High, DocumentStatus.AwaitingConfirmation)]
    [InlineData("contract_appointment", ConfidenceBand.High, DocumentStatus.AwaitingConfirmation)]
    [InlineData("approval_legal", ConfidenceBand.High, DocumentStatus.AwaitingConfirmation)]
    [InlineData("payment", ConfidenceBand.Low, DocumentStatus.NeedsReview)]
    [InlineData("needs_review", ConfidenceBand.High, DocumentStatus.NeedsReview)]
    public void Decide_FilesOnlySureLowRiskDocumentsAndAlwaysAsksForHighConsequenceOnes(string categoryKey, ConfidenceBand band, DocumentStatus expected)
    {
        FilingDecider.Decide(band, SampleDocuments.Category(categoryKey)).ShouldBe(expected);
    }
}
