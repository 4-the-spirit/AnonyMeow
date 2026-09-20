using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class PiiDetectionLogConfiguration : IEntityTypeConfiguration<PiiDetectionLog>
{
    public void Configure(EntityTypeBuilder<PiiDetectionLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.MatchedCategory).IsRequired();
        builder.Property(l => l.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // No TargetId/FK to the actual Post/Comment/Message is modeled — this log tracks
        // per-submission-attempt detection outcomes, and a blocked submission may never end up
        // persisted anywhere to link back to. Same "no FK, service-layer only" spirit as
        // Vote/Report/ModerationAction/Notification.
    }
}
