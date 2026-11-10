namespace Tumiia.Domain.Analysis;

/// <summary>The standard fields TUMIIA tries to find in every document (v1.1 section 3 and the August brief section 4).</summary>
public static class FieldNames
{
    public const string DocumentType = "document_type";
    public const string Subject = "subject";
    public const string Party = "party";
    public const string DocumentDate = "document_date";
    public const string Amount = "amount";
    public const string Currency = "currency";
    public const string WorkPackage = "work_package";
    public const string SiteReference = "site_reference";
    public const string Location = "location";
    public const string Language = "language";
}
