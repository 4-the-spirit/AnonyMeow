using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class SpamFlagConfiguration : IEntityTypeConfiguration<SpamFlag>
{
    public void Configure(EntityTypeBuilder<SpamFlag> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(f => f.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Report>()
            .WithMany()
            .HasForeignKey(f => f.ReportId)
            .OnDelete(DeleteBehavior.Restrict);

        // No FK on TargetId — polymorphic (Post/Comment/DirectMessage), same pattern as Report.
    }
}
