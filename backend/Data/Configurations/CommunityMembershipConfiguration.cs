using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class CommunityMembershipConfiguration : IEntityTypeConfiguration<CommunityMembership>
{
    public void Configure(EntityTypeBuilder<CommunityMembership> builder)
    {
        builder.HasKey(m => new { m.CommunityId, m.AppUserId });

        builder.Property(m => m.JoinedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<Community>()
            .WithMany()
            .HasForeignKey(m => m.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
