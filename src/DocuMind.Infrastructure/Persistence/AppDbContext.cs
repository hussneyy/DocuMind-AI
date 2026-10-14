using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for DocuMind. Entities (Document, DocumentChunk, Conversation, etc.)
/// are added in later milestones as the corresponding features are implemented.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");
    }
}
