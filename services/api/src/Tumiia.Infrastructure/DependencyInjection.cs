using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tumiia.Application.Auth;
using Tumiia.Application.Documents;
using Tumiia.Application.Projects;
using Tumiia.Domain.Email;
using Tumiia.Domain.Storage;
using Tumiia.Infrastructure.Aws;
using Tumiia.Infrastructure.Audit;
using Tumiia.Infrastructure.Email;
using Tumiia.Infrastructure.Identity;
using Tumiia.Infrastructure.Persistence;
using Tumiia.Infrastructure.Storage;

namespace Tumiia.Infrastructure;

/// <summary>Registers the Infrastructure services with the app.</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string in configuration (<c>ConnectionStrings:Tumiia</c>).</summary>
    public const string ConnectionStringName = "Tumiia";

    /// <summary>
    /// Adds the database, ASP.NET Core Identity, access tokens, the stores behind the use cases, AWS (S3, SQS, SES)
    /// and email.
    /// The connection string is read when the database is first used, so the app can still start (for example
    /// to generate the OpenAPI file) without one. See <see cref="DatabaseConnectionString"/> for where it comes from.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.TryAddClock();
        AddDatabase(services, configuration);
        AddIdentity(services);
        AddAccessTokens(services, configuration, environment);
        AddAws(services, configuration);
        AddEmail(services, configuration);

        services.AddScoped<IUserAccounts, IdentityUserAccounts>();
        services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
        services.AddScoped<IAuditLog, EfAuditLog>();
        services.AddScoped<IProjectStore, EfProjectStore>();
        services.AddScoped<IDocumentStore, EfDocumentStore>();
        return services;
    }

    /// <summary>
    /// Brings the database up to the latest migration. Only call this in Development:
    /// in AWS, migrations run as a separate one-off task before the new version starts.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TumiiaDbContext>().Database;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TumiiaDbContext>>();

        logger.LogInformation("Applying database migrations");
        try
        {
            await database.MigrateAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogCritical(exception, "Could not apply database migrations. Is PostgreSQL running? From the repo root run: docker compose up -d");
            throw;
        }
    }

    private static void TryAddClock(this IServiceCollection services)
    {
        if (services.All(service => service.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TumiiaDbContext>(options =>
        {
            var connectionString = DatabaseConnectionString.Resolve(configuration)
                ?? throw new InvalidOperationException(
                    $"No database configured. Set 'ConnectionStrings:{ConnectionStringName}', or the '{DatabaseConnectionString.SectionName}:Host' settings.");
            TumiiaDbContextOptions.Configure(options, connectionString);
        });
    }

    /// <summary>
    /// Identity with Guid ids and roles. Password reset tokens come from Data Protection and last one hour; the
    /// Data Protection keys live in PostgreSQL so tokens keep working across restarts and redeploys (except in the
    /// build's OpenAPI generation, which has no database).
    /// </summary>
    private static void AddIdentity(IServiceCollection services)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("tumiia");
        if (!BuildTimeDocumentGeneration.IsRunning)
        {
            dataProtection.PersistKeysToDbContext<TumiiaDbContext>();
        }

        services.AddIdentityCore<ApplicationUser>(IdentityRules.Configure)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TumiiaDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));
    }

    /// <summary>
    /// JWT settings from the <c>Jwt</c> section, checked when the app starts, so a missing key stops a real
    /// deployment straight away. In Development (and during the build's OpenAPI generation), when no key is set, a
    /// random key is made, so no key ever needs to be committed.
    /// </summary>
    private static void AddAccessTokens(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .PostConfigure(settings =>
            {
                var needsThrowawayKey = environment.IsDevelopment() || BuildTimeDocumentGeneration.IsRunning;
                if (needsThrowawayKey && string.IsNullOrEmpty(settings.SigningKey))
                {
                    settings.SigningKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
    }

    /// <summary>
    /// S3 for original files and SQS for analysis requests. The clients are created on first use, so the app can
    /// start without AWS (for example to generate the OpenAPI file).
    /// </summary>
    private static void AddAws(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsSettings>(configuration.GetSection(AwsSettings.SectionName));
        services.Configure<StorageSettings>(settings =>
        {
            settings.DocumentsBucket = configuration["Storage:DocumentsBucket"] ?? string.Empty;
            settings.DocumentAnalysisQueueUrl = configuration["Queues:DocumentAnalysisUrl"] ?? string.Empty;
        });

        services.AddSingleton(provider => AwsClients.CreateS3(provider.GetRequiredService<IOptions<AwsSettings>>().Value));
        services.AddSingleton(provider => AwsClients.CreateSqs(provider.GetRequiredService<IOptions<AwsSettings>>().Value));
        services.AddSingleton<IFileStore, S3FileStore>();
        services.AddSingleton<IDocumentQueue, SqsDocumentQueue>();
    }

    /// <summary>SES when <c>Email:Provider</c> is "Ses" (as in AWS); otherwise the local file-writing sender.</summary>
    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var emailSection = configuration.GetSection(EmailSettings.SectionName);
        services.Configure<EmailSettings>(emailSection);

        var settings = emailSection.Get<EmailSettings>() ?? new EmailSettings();
        if (!settings.UsesSes)
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
            return;
        }

        services.AddSingleton(provider => AwsClients.CreateSes(provider.GetRequiredService<IOptions<AwsSettings>>().Value));
        services.AddSingleton<IEmailSender, SesEmailSender>();
    }
}
