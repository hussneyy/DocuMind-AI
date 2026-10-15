using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(builder =>
        {
            builder.ToTable("Documents");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
            builder.Property(d => d.StorageKey).HasMaxLength(255).IsRequired();
            builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(d => d.FileHash).HasMaxLength(64).IsRequired();
            builder.HasIndex(d => d.OwnerUserId);
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(builder =>
        {
            builder.ToTable("DocumentChunks");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.TextContent).IsRequired();
            builder.Property(c => c.EmbeddingModel).HasMaxLength(100);

            // Deliberately no fixed dimension: different embedding providers/models use different
            // vector sizes, and switching providers shouldn't require a column migration. The
            // trade-off is that mixing dimensions within one similarity query would error at the
            // database level — the app avoids that by always re-embedding with the currently
            // configured model (see DocumentIndexer), so retry after a model change is required.
            builder.Property(c => c.Embedding).HasColumnType("vector");

            builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex }).IsUnique();
            builder.HasOne<Document>()
                .WithMany()
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
