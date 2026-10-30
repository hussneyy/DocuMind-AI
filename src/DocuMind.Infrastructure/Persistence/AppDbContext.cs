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
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageCitation> MessageCitations => Set<MessageCitation>();
    public DbSet<DocumentSummary> DocumentSummaries => Set<DocumentSummary>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();

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

        modelBuilder.Entity<Conversation>(builder =>
        {
            builder.ToTable("Conversations");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
            builder.HasIndex(c => c.OwnerUserId);
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(builder =>
        {
            builder.ToTable("Messages");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Content).IsRequired();
            builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
            builder.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageCitation>(builder =>
        {
            builder.ToTable("MessageCitations");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.DocumentFileName).HasMaxLength(255).IsRequired();
            builder.Property(c => c.Excerpt).IsRequired();
            builder.HasIndex(c => c.MessageId);
            builder.HasOne<Message>()
                .WithMany()
                .HasForeignKey(c => c.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentSummary>(builder =>
        {
            builder.ToTable("DocumentSummaries");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Content).IsRequired();
            builder.Property(s => s.LlmModel).HasMaxLength(100).IsRequired();
            builder.HasIndex(s => new { s.DocumentId, s.SummaryType }).IsUnique();
            builder.HasOne<Document>()
                .WithMany()
                .HasForeignKey(s => s.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(builder =>
        {
            builder.ToTable("Tags");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).HasMaxLength(50).IsRequired();
            builder.HasIndex(t => t.OwnerUserId);
            builder.HasIndex(t => new { t.OwnerUserId, t.Name }).IsUnique();
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentTag>(builder =>
        {
            builder.ToTable("DocumentTags");
            builder.HasKey(dt => new { dt.DocumentId, dt.TagId });
            builder.HasOne<Document>()
                .WithMany()
                .HasForeignKey(dt => dt.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Tag>()
                .WithMany()
                .HasForeignKey(dt => dt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
