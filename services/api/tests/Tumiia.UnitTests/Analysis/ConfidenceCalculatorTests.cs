using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class ConfidenceCalculatorTests
{
    private static readonly ConfidenceWeights Weights = new();
    private static readonly ConfidenceSignals AllPerfect = new(1, 1, 1, 1, 1, 1);

    private static ConfidenceInput Input(
        ConfidenceSignals? signals = null,
        bool rulesOnly = false,
        bool almostNoText = false,
        DetectedFormat format = DetectedFormat.Pdf,
        bool camera = false)
    {
        return new ConfidenceInput(signals ?? AllPerfect, almostNoText, new FileFormat(format, "x", true), camera, 0.75, 0.50, rulesOnly);
    }

    [Fact]
    public void Calculate_AddsUpTheWeightedSignals()
    {
        var signals = new ConfidenceSignals(WinnerProbability: 0.6, Margin: 0.5, Readability: 1, Agreement: 0.5, ExpectedFieldsFound: 1, FormatMatches: 1);

        var confidence = ConfidenceCalculator.Calculate(Input(signals), Weights);

        confidence.Score.ShouldBe((0.6 * 0.30) + (0.5 * 0.15) + 0.15 + (0.5 * 0.20) + 0.10 + 0.10, 0.0001);
        confidence.Band.ShouldBe(ConfidenceBand.Medium);
        confidence.Signals.ShouldBe(signals);
        confidence.CapsApplied.ShouldBeEmpty();
    }

    [Fact]
    public void Calculate_WhenEverythingPointsTheSameWay_IsHigh()
    {
        var confidence = ConfidenceCalculator.Calculate(Input(), Weights);

        confidence.Score.ShouldBe(1);
        confidence.Band.ShouldBe(ConfidenceBand.High);
    }

    [Fact]
    public void Calculate_WhenOnlyTheKeywordRulesClassifiedIt_IsLowSoAPersonLooks()
    {
        var confidence = ConfidenceCalculator.Calculate(Input(rulesOnly: true), Weights);

        confidence.Band.ShouldBe(ConfidenceBand.Low);
        confidence.CapsApplied.ShouldHaveSingleItem().ShouldContain("keywords only");
    }

    [Fact]
    public void Calculate_WhenThereIsAlmostNoText_IsLow()
    {
        ConfidenceCalculator.Calculate(Input(almostNoText: true), Weights).Band.ShouldBe(ConfidenceBand.Low);
    }

    [Fact]
    public void Calculate_WhenADrawingHasLittleText_IsNotLoweredForIt()
    {
        ConfidenceCalculator.Calculate(Input(almostNoText: true, format: DetectedFormat.Dxf), Weights).Band.ShouldBe(ConfidenceBand.High);
    }

    [Fact]
    public void Calculate_WhenACameraPhotoHasNoText_IsMediumEvenIfTheSignalsAreWeak()
    {
        var weak = new ConfidenceSignals(0.1, 0, 0, 0.5, 0, 1);

        ConfidenceCalculator.Calculate(Input(weak, almostNoText: true, format: DetectedFormat.Jpeg, camera: true), Weights).Band.ShouldBe(ConfidenceBand.Medium);
        ConfidenceCalculator.Calculate(Input(almostNoText: true, format: DetectedFormat.Jpeg, camera: true), Weights).Band.ShouldBe(ConfidenceBand.Medium);
    }

    [Fact]
    public void Calculate_WhenAnImageHasNoTextAndNoCameraData_IsLow()
    {
        ConfidenceCalculator.Calculate(Input(almostNoText: true, format: DetectedFormat.Png), Weights).Band.ShouldBe(ConfidenceBand.Low);
    }

    [Fact]
    public void Calculate_WhenTheFileIsADwg_StaysBelowTheDwgCap()
    {
        var confidence = ConfidenceCalculator.Calculate(Input(format: DetectedFormat.Dwg), Weights);

        confidence.Score.ShouldBe(ConfidenceCalculator.DwgCap);
    }

    [Theory]
    [InlineData(0.75, ConfidenceBand.High)]
    [InlineData(0.7499, ConfidenceBand.Medium)]
    [InlineData(0.50, ConfidenceBand.Medium)]
    [InlineData(0.4999, ConfidenceBand.Low)]
    public void BandFor_UsesTheThresholdsInclusively(double score, ConfidenceBand expected)
    {
        ConfidenceCalculator.BandFor(score, 0.75, 0.50).ShouldBe(expected);
    }
}
