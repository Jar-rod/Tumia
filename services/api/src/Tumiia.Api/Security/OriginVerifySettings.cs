namespace Tumiia.Api.Security;

/// <summary>The origin check settings, from the <c>OriginVerify</c> section. In AWS the secret comes from Secrets Manager.</summary>
public sealed class OriginVerifySettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "OriginVerify";

    /// <summary>The value CloudFront sends in <c>X-Origin-Verify</c>.</summary>
    public string? Secret { get; set; }
}
