using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(30);
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // SearchVector (Phase 10 full-text search) is configured in AppDbContext.OnModelCreating
        // — see PostConfiguration for why.
    }
}
