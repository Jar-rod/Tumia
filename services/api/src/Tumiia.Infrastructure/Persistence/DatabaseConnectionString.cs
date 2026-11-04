using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// Works out the PostgreSQL connection string from configuration. Locally a whole connection string is set
/// (<c>ConnectionStrings:Tumiia</c>). In AWS the RDS secret supplies the parts separately
/// (<c>Database:Host</c>, <c>Database:Port</c>, <c>Database:Name</c>, <c>Database:Username</c>,
/// <c>Database:Password</c>), so they are joined here.
/// </summary>
public static class DatabaseConnectionString
{
    /// <summary>The configuration section that holds the separate connection parts.</summary>
    public const string SectionName = "Database";

    private const int DefaultPort = 5432;

    /// <summary>
    /// Returns the full connection string when one is set; otherwise builds one from the parts, requiring an
    /// encrypted (TLS) connection. Returns null when neither is configured.
    /// </summary>
    public static string? Resolve(IConfiguration configuration)
    {
        var fullConnectionString = configuration.GetConnectionString(DependencyInjection.ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(fullConnectionString))
        {
            return fullConnectionString;
        }

        var parts = configuration.GetSection(SectionName);
        if (string.IsNullOrWhiteSpace(parts["Host"]))
        {
            return null;
        }

        return BuildFromParts(parts);
    }

    /// <summary>Joins the separate connection parts into one Npgsql connection string.</summary>
    private static string BuildFromParts(IConfigurationSection parts)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = parts["Host"],
            Port = parts.GetValue("Port", DefaultPort),
            Database = parts["Name"],
            Username = parts["Username"],
            Password = parts["Password"],
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }
}
