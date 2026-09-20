using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Emoji).IsRequired();
        builder.Property(r => r.CreatedAtUtc).HasDefaultValueSql("now()");

        // A viewer may hold at most one active reaction per target — enforced here (unique on
        // user+target, not user+target+emoji) and in ReactionService.AddAsync, which replaces
        // any existing different-emoji reaction rather than allowing both to coexist.
        builder.HasIndex(r => new { r.TargetType, r.TargetId, r.AppUserId }).IsUnique();

        // No DB-level FK on TargetId — it's polymorphic (Post or Comment), enforced at the
        // service layer instead, same pattern as Vote/Report.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(r => r.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
