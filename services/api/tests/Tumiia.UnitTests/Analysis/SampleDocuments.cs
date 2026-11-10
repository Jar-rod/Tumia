using Tumiia.Domain.Analysis;
using Tumiia.Infrastructure.Persistence;

namespace Tumiia.UnitTests.Analysis;

/// <summary>Synthetic document text and the real seed taxonomy, shared by the analysis tests. No real client data.</summary>
public static class SampleDocuments
{
    public static readonly IReadOnlyList<TaxonomyCategory> Taxonomy = TaxonomySeed.Load();

    public const string Quote = """
        ABC Supplies (Pty) Ltd
        QUOTATION No: Q-1042
        Date: 12/10/2026
        Client: Mokoena Residence
        Site: Erf 1234, Midrand
        Description of works - Foundation brickwork
        Item | Qty | Price
        Bricks NFX | 20000 | R 15 000.00
        Cement 50kg | 100 | R 3 450,00
        Total R 18,450.00
        Valid for 30 days
        """;

    public const string Payment = """
        WorldRemit
        Payment successful
        Recipient: Jean Habimana
        Amount paid: €2,000.00
        Transaction ID: WR-88213
        Date: 14 Aug 2026
        Proof of payment
        """;

    public const string Payslip = """
        Kigali Hardware Ltd
        PAYSLIP for October 2026
        Gross pay R 25 000.00
        UIF R 177.12
        CTC R 27 500.00
        Pay date: 25/10/2026
        """;

    public static TaxonomyCategory Category(string key) => Taxonomy.Single(category => category.Key == key);

    /// <summary>Typed text, one page, every line read with full confidence.</summary>
    public static DocumentText Typed(string text, double confidence = 1.0) => new()
    {
        Pages = [new TextPage(1, text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).Select(line => new TextLine(line, confidence)).ToList())],
    };

    /// <summary>A hosted AI answer for a document, with the given category, confidence and facts.</summary>
    public static HostedAnalysis Hosted(string category, double confidence = 0.95, string? runnerUp = null, double runnerUpConfidence = 0.03, double readability = 0.95, params HostedField[] fields) =>
        new(category, confidence, runnerUp, runnerUpConfidence, readability, "en", fields, [], "anthropic.claude-haiku-5-5");

    public static FileFormat Pdf(bool extensionMatches = true) => new(DetectedFormat.Pdf, "application/pdf", extensionMatches);

    public static ExtractedField? Field(IEnumerable<ExtractedField> fields, string name) => fields.FirstOrDefault(field => field.Name == name);
}
