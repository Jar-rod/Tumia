namespace Tumiia.Domain.Analysis;

/// <summary>
/// Works out how sure TUMIIA is about a document's category, from 0 (no idea) to 1 (certain). It is a weighted sum of
/// the signals, then limited by a few rules: when only the keyword rules classified it, it stays Low (Needs Review);
/// with almost nothing readable it is Low (except for drawings and photos); a photo with nothing readable is Medium
/// when a camera took it and Low otherwise; and a DWG drawing, whose content cannot be read, stays below 0.85.
/// </summary>
public static class ConfidenceCalculator
{
    /// <summary>The highest score a DWG drawing can get.</summary>
    public const double DwgCap = 0.84;

    private const double JustBelow = 0.0001;

    /// <summary>Calculates the score and its band.</summary>
    public static ClassificationConfidence Calculate(ConfidenceInput input, ConfidenceWeights weights)
    {
        var signals = input.Signals;
        var score = (signals.WinnerProbability * weights.WinnerProbability)
            + (signals.Margin * weights.Margin)
            + (signals.Readability * weights.Readability)
            + (signals.Agreement * weights.Agreement)
            + (signals.ExpectedFieldsFound * weights.ExpectedFieldsFound)
            + (signals.FormatMatches * weights.FormatMatches);

        var caps = new List<string>();
        var belowHigh = input.HighThreshold - JustBelow;
        var belowMedium = input.MediumThreshold - JustBelow;

        if (input.RulesOnly && score > belowMedium)
        {
            score = belowMedium;
            caps.Add("Sorted by keywords only, because the AI did not read this document.");
        }

        if (input.Format.IsImage && input.AlmostNoText)
        {
            score = input.HasCameraData ? Math.Clamp(score, input.MediumThreshold, belowHigh) : Math.Min(score, belowMedium);
            caps.Add(input.HasCameraData ? "A photo with nothing readable: at most fairly sure." : "An image with nothing readable and no camera data.");
        }
        else if (input.AlmostNoText && !input.Format.IsCad && score > belowMedium)
        {
            score = belowMedium;
            caps.Add("Almost nothing in it could be read.");
        }

        if (input.Format.Format == DetectedFormat.Dwg && score > DwgCap)
        {
            score = DwgCap;
            caps.Add("DWG content cannot be read, only its format and name.");
        }

        score = Math.Round(Math.Clamp(score, 0, 1), 4);
        return new ClassificationConfidence(score, BandFor(score, input.HighThreshold, input.MediumThreshold), signals, caps);
    }

    /// <summary>The band a score falls in, given the category's thresholds.</summary>
    public static ConfidenceBand BandFor(double score, double highThreshold, double mediumThreshold)
    {
        if (score >= highThreshold)
        {
            return ConfidenceBand.High;
        }

        return score >= mediumThreshold ? ConfidenceBand.Medium : ConfidenceBand.Low;
    }
}
