using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class PollVoteConfiguration : IEntityTypeConfiguration<PollVote>
{
    public void Configure(EntityTypeBuilder<PollVote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.PostId, v.AppUserId }).IsUnique();

        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(v => v.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(v => v.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PollOption>()
            .WithMany()
            .HasForeignKey(v => v.PollOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
