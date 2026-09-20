using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class CommunityRuleConfiguration : IEntityTypeConfiguration<CommunityRule>
{
    public void Configure(EntityTypeBuilder<CommunityRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(500);
        builder.Property(r => r.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasIndex(r => new { r.CommunityId, r.Order });

        builder.HasOne<Community>()
            .WithMany()
            .HasForeignKey(r => r.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
