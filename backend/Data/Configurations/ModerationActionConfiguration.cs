using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(a => a.ModId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Community>()
            .WithMany()
            .HasForeignKey(a => a.CommunityId)
            .OnDelete(DeleteBehavior.Restrict);

        // No FK on TargetId — polymorphic across Post/Comment/user targets depending on ActionType.
    }
}
