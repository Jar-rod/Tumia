using Tumiia.Application;
using Tumiia.Application.Auth;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;

namespace Tumiia.Api.Startup;

/// <summary>Registers the use cases and the settings they need.</summary>
public static class ApplicationSetup
{
    private const string LocalAppAddress = "http://localhost:5173";

    /// <summary>Adds every use case (one instance per request) and builds <see cref="AuthSettings"/>, the upload rules and the confidence weights from configuration.</summary>
    public static IServiceCollection AddTumiiaApplication(this IServiceCollection services)
    {
        foreach (var useCase in ApplicationUseCases.All)
        {
            services.AddScoped(useCase);
        }

        services.AddSingleton(provider => ReadAuthSettings(
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IHostEnvironment>()));
        services.AddSingleton(provider => ReadUploadRules(provider.GetRequiredService<IConfiguration>()));
        services.AddSingleton(provider => ReadConfidenceWeights(provider.GetRequiredService<IConfiguration>()));

        return services;
    }

    /// <summary>
    /// <c>App:BaseUrl</c> is the address used in emailed links (the Vite dev server when developing locally).
    /// <c>Admin:Emails</c> is a comma-separated list, or a list in appsettings.
    /// </summary>
    public static AuthSettings ReadAuthSettings(IConfiguration configuration, IHostEnvironment environment)
    {
        var baseUrl = configuration["App:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = environment.IsDevelopment()
                ? LocalAppAddress
                : throw new InvalidOperationException("App:BaseUrl is not set. It is needed for the links in emails.");
        }

        return new AuthSettings
        {
            AppBaseUrl = baseUrl,
            AdminEmails = ReadAdminEmails(configuration),
            RefreshTokenLifetime = TimeSpan.FromDays(configuration.GetValue("Auth:RefreshTokenDays", 14)),
        };
    }

    /// <summary>
    /// The <c>Uploads</c> section: allowed extensions (with or without the dot, any case), the largest file in MB and
    /// the most files per request. Defaults: the v1.1 file types, 50 MB and 50 files.
    /// </summary>
    public static UploadRules ReadUploadRules(IConfiguration configuration)
    {
        var section = configuration.GetSection("Uploads");
        var extensions = section.GetSection("AllowedExtensions").Get<string[]>() ?? [];

        return new UploadRules
        {
            AllowedExtensions = extensions
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => "." + extension.Trim().TrimStart('.').ToLowerInvariant())
                .ToHashSet(),
            MaxFileSizeMb = section.GetValue("MaxFileSizeMb", 50),
            MaxFilesPerRequest = section.GetValue("MaxFilesPerRequest", 50),
        };
    }

    /// <summary>
    /// The <c>Analysis:Weights</c> section: how much each signal counts towards the classification confidence. Any
    /// weight left out keeps its default from the build plan.
    /// </summary>
    public static ConfidenceWeights ReadConfidenceWeights(IConfiguration configuration)
    {
        return configuration.GetSection("Analysis:Weights").Get<ConfidenceWeights>() ?? new ConfidenceWeights();
    }

    private static HashSet<string> ReadAdminEmails(IConfiguration configuration)
    {
        var section = configuration.GetSection("Admin:Emails");
        var listed = section.GetChildren().Select(child => child.Value);
        var commaSeparated = (section.Value ?? string.Empty).Split(',');

        return listed.Concat(commaSeparated)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
