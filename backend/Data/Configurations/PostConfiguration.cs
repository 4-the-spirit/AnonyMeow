using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(300);
        builder.Property(p => p.BodyMarkdown).HasMaxLength(40_000);
        builder.Property(p => p.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.CommunityId, p.CreatedAtUtc });

        builder.HasOne<Community>()
            .WithMany()
            .HasForeignKey(p => p.CommunityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Flair>()
            .WithMany()
            .HasForeignKey(p => p.FlairId)
            .OnDelete(DeleteBehavior.SetNull);

        // SearchVector (Phase 10 full-text search) is configured in AppDbContext.OnModelCreating,
        // not here — it's an Npgsql-only generated column type (NpgsqlTsVector) that the
        // InMemory provider used by unit tests can't map, so it needs a provider check that a
        // plain IEntityTypeConfiguration doesn't have access to.
    }
}
