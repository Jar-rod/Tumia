namespace Tumiia.Domain.Analysis;

/// <summary>How a field's value was found. Stored with each field. <c>hosted</c> means the hosted AI (Bedrock) found it.</summary>
public static class FieldMethods
{
    public const string Regex = "regex";
    public const string Keyword = "keyword";
    public const string Model = "model";
    public const string Exif = "exif";
    public const string Header = "header";
    public const string Hosted = "hosted";
}
