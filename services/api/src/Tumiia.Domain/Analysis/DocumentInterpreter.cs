namespace Tumiia.Domain.Analysis;

/// <summary>
/// The thinking part of the analysis, with no file or network access. Normally the hosted AI (Claude on Amazon
/// Bedrock) has already read the document: its category and facts are checked here, scored and turned into a
/// filing decision. When the AI was not used (switched off, or it failed), the keyword rules classify the document
/// instead, and their confidence is kept below Medium, so it goes to Needs Review rather than being filed on keywords.
/// </summary>
public static class DocumentInterpreter
{
    /// <summary>Below this many letters and digits, typed text counts as almost none.</summary>
    public const int AlmostNoTextCharacters = 20;

    /// <summary>Below this readability, the hosted AI could hardly read the document.</summary>
    public const double AlmostUnreadable = 0.2;

    private const int FullyReadableCharacters = 200;

    private static readonly TaxonomyCategory FallbackNeedsReview = new() { Key = TaxonomyCategory.NeedsReviewKey, DisplayName = "Needs Review" };

    /// <summary>Interprets one document.</summary>
    public static Interpretation Interpret(InterpretationRequest request)
    {
        var classifiedText = string.Join('\n', new[] { request.Text.AllText }.Concat(request.Text.FileHeaderFacts));
        var rules = CategoryChooser.Choose(RulesClassifier.Score(classifiedText, request.FileName, request.Categories));

        var hostedCategory = request.Hosted is null ? null : request.Categories.FirstOrDefault(category => category.Key == request.Hosted.CategoryKey);
        return request.Hosted is not null && hostedCategory is not null
            ? InterpretHosted(request, request.Hosted, hostedCategory, rules)
            : InterpretByRules(request, rules);
    }

    private static Interpretation InterpretHosted(InterpretationRequest request, HostedAnalysis hosted, TaxonomyCategory category, CategoryChoice rules)
    {
        var language = IsLanguageCode(hosted.Language) ? hosted.Language.ToLowerInvariant() : LanguageDetector.Detect(request.Text.AllText);
        var (checkedFields, fieldWarnings) = HostedFieldReader.Read(hosted.Fields, request.Text);
        var fields = checkedFields.ToList();
        AddMissing(fields, FieldExtractor.PhotoFields(request.Text.Image));
        AddMissing(fields, FieldExtractor.LanguageField(language));

        var readability = Math.Clamp(hosted.Readability, 0, 1);
        var winner = Math.Clamp(hosted.Confidence, 0, 1);
        var signals = new ConfidenceSignals(
            WinnerProbability: winner,
            Margin: Math.Max(0, winner - Math.Clamp(hosted.RunnerUpConfidence, 0, 1)),
            Readability: readability,
            Agreement: !rules.HasOpinion ? 0.5 : rules.WinnerKey == category.Key ? 1 : 0,
            ExpectedFieldsFound: ExpectedFieldsFound(category, fields),
            FormatMatches: request.Format.ExtensionMatches ? 1 : 0);
        var confidence = Score(request, category, signals, almostNoText: readability < AlmostUnreadable, rulesOnly: false);

        fields.Insert(0, new ExtractedField(FieldNames.DocumentType, category.DisplayName, confidence.Score, new FieldEvidence("bedrock"), FieldMethods.Hosted));
        var warnings = request.Text.Warnings.Concat(hosted.Warnings).Concat(fieldWarnings);
        return Finish(category, hosted.RunnerUpKey, language, confidence, fields, warnings);
    }

    private static Interpretation InterpretByRules(InterpretationRequest request, CategoryChoice rules)
    {
        var category = FindCategory(request.Categories, rules.WinnerKey);
        var language = LanguageDetector.Detect(request.Text.AllText);
        var fields = FieldExtractor.Extract(request.Text, category, request.Categories, language).ToList();

        var characters = request.Text.CharacterCount;
        var signals = new ConfidenceSignals(
            WinnerProbability: rules.WinnerProbability,
            Margin: Math.Max(0, rules.WinnerProbability - rules.RunnerUpProbability),
            Readability: Math.Min(1.0, characters / (double)FullyReadableCharacters) * request.Text.AverageConfidence,
            Agreement: 0.5,
            ExpectedFieldsFound: ExpectedFieldsFound(category, fields),
            FormatMatches: request.Format.ExtensionMatches ? 1 : 0);
        var confidence = Score(request, category, signals, almostNoText: characters < AlmostNoTextCharacters, rulesOnly: true);

        fields.Insert(0, new ExtractedField(FieldNames.DocumentType, category.DisplayName, confidence.Score, new FieldEvidence("classification"), FieldMethods.Keyword));
        return Finish(category, rules.RunnerUpKey, language, confidence, fields, request.Text.Warnings);
    }

    private static ClassificationConfidence Score(InterpretationRequest request, TaxonomyCategory category, ConfidenceSignals signals, bool almostNoText, bool rulesOnly)
    {
        var input = new ConfidenceInput(
            signals,
            almostNoText,
            request.Format,
            HasCameraData: request.Text.Image?.HasCameraData == true,
            category.HighThreshold,
            category.MediumThreshold,
            rulesOnly);

        var confidence = ConfidenceCalculator.Calculate(input, request.Weights);
        return category.IsNeedsReview ? confidence with { Band = ConfidenceBand.Low } : confidence;
    }

    private static Interpretation Finish(TaxonomyCategory category, string? runnerUp, string language, ClassificationConfidence confidence, List<ExtractedField> fields, IEnumerable<string> warnings)
    {
        var outcome = FilingDecider.Decide(confidence.Band, category);
        var summary = category.IsNeedsReview ? "I'm not sure what this is." : SummaryWriter.Write(category.DisplayName, fields);
        var allWarnings = warnings.Concat(confidence.CapsApplied).ToList();
        if (language is not ("en" or LanguageDetector.Undetermined))
        {
            allWarnings.Add($"The text looks like it is not in English (language: {language}). Check the details.");
        }

        return new Interpretation(category, runnerUp, language, confidence, outcome, fields, summary, allWarnings);
    }

    private static TaxonomyCategory FindCategory(IReadOnlyList<TaxonomyCategory> categories, string key)
    {
        return categories.FirstOrDefault(category => category.Key == key)
            ?? categories.FirstOrDefault(category => category.IsNeedsReview)
            ?? FallbackNeedsReview;
    }

    private static double ExpectedFieldsFound(TaxonomyCategory category, List<ExtractedField> fields)
    {
        if (category.ExpectedFields.Count == 0)
        {
            return 1;
        }

        var found = category.ExpectedFields.Count(expected => fields.Any(field => field.Name == expected));
        return found / (double)category.ExpectedFields.Count;
    }

    // Photo date and position come from EXIF, which TUMIIA reads itself; they fill in what the AI did not give.
    private static void AddMissing(List<ExtractedField> fields, IEnumerable<ExtractedField> extra)
    {
        foreach (var field in extra.Where(field => fields.All(existing => existing.Name != field.Name)))
        {
            fields.Add(field);
        }
    }

    private static bool IsLanguageCode(string? code) => code is { Length: 2 } && code.All(char.IsAsciiLetter);
}
