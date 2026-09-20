using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.TargetType, v.TargetId, v.VoterId }).IsUnique();

        // No DB-level FK on TargetId — it's polymorphic (Post or Comment), enforced at the
        // service layer instead, same pattern the parent plan uses for Report/Reaction later.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(v => v.VoterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
