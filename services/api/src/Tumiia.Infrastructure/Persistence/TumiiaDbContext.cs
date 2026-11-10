using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Audit;
using Tumiia.Domain.Auth;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Projects;
using Tumiia.Infrastructure.Identity;
using Tumiia.Infrastructure.Persistence.Configurations;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context for TUMIIA: projects, their documents and the documents' analyses, the taxonomy, Identity's user and role tables,
/// refresh tokens, the audit log and the
/// ASP.NET Data Protection keys (so password reset tokens survive restarts and redeploys).
/// Table and column names are snake_case (set in <see cref="TumiiaDbContextOptions"/>).
/// </summary>
public sealed class TumiiaDbContext(DbContextOptions<TumiiaDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentAnalysis> DocumentAnalyses => Set<DocumentAnalysis>();

    public DbSet<TaxonomyCategoryRecord> TaxonomyCategories => Set<TaxonomyCategoryRecord>();

    /// <summary>Data Protection keys, used to protect password reset tokens.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>Applies the table mappings in Persistence/Configurations, names Identity's tables, and seeds the roles.</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TumiiaDbContext).Assembly);
        IdentityTables.Configure(builder);
    }
}
