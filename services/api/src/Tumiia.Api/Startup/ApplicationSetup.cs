using Tumiia.Application;
using Tumiia.Application.Auth;

namespace Tumiia.Api.Startup;

/// <summary>Registers the use cases and the settings they need.</summary>
public static class ApplicationSetup
{
    private const string LocalAppAddress = "http://localhost:5173";

    /// <summary>Adds every use case (one instance per request) and builds <see cref="AuthSettings"/> from configuration.</summary>
    public static IServiceCollection AddTumiiaApplication(this IServiceCollection services)
    {
        foreach (var useCase in ApplicationUseCases.All)
        {
            services.AddScoped(useCase);
        }

        services.AddSingleton(provider => ReadAuthSettings(
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IHostEnvironment>()));

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
