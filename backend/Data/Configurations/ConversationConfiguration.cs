using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CreatedAtUtc).HasDefaultValueSql("now()");

        // Uniqueness relies on ConversationService always normalizing the participant pair into a
        // deterministic (lower id, higher id) order before find-or-create.
        builder.HasIndex(c => new { c.ParticipantAId, c.ParticipantBId }).IsUnique();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.ParticipantAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.ParticipantBId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
