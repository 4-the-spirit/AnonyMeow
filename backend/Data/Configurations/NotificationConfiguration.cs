using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.PreviewText).IsRequired();
        builder.Property(n => n.CreatedAtUtc).HasDefaultValueSql("now()");
        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAtUtc });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        // No FK on SourceId — polymorphic (Comment, ModerationAction, or User), enforced at the
        // service layer, same pattern as Vote/Report/ModerationAction.
    }
}
