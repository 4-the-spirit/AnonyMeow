using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class CommunityBanConfiguration : IEntityTypeConfiguration<CommunityBan>
{
    public void Configure(EntityTypeBuilder<CommunityBan> builder)
    {
        builder.HasKey(b => new { b.CommunityId, b.AppUserId });
        builder.Property(b => b.Reason).IsRequired();
        builder.Property(b => b.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<Community>()
            .WithMany()
            .HasForeignKey(b => b.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(b => b.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(b => b.BannedByModId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
