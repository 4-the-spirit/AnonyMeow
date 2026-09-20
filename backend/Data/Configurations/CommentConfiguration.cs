using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.BodyMarkdown).IsRequired().HasMaxLength(10_000);
        builder.Property(c => c.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not Cascade — Postgres/EF reject multiple cascade paths reaching the same
        // table, and a self-referencing FK plus the Post/AppUser FKs above would create one.
        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // SearchVector (Phase 10 full-text search) is configured in AppDbContext.OnModelCreating
        // — see PostConfiguration for why.
    }
}
