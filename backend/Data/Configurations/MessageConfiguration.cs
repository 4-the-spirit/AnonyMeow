using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Body).IsRequired();
        builder.Property(m => m.CreatedAtUtc).HasDefaultValueSql("now()");
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAtUtc });

        // Real, single-type FK (not polymorphic) — messages have no independent lifecycle from
        // their conversation, so Cascade is intended here, unlike the Restrict-only AppUser FKs.
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing and optional — Restrict (not Cascade) so deleting/removing the
        // replied-to message can never cascade into deleting the reply itself.
        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
