using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class PlatformRestrictionConfiguration : IEntityTypeConfiguration<PlatformRestriction>
{
    public void Configure(EntityTypeBuilder<PlatformRestriction> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).IsRequired();
        builder.Property(r => r.StartAtUtc).HasDefaultValueSql("now()");
        builder.HasIndex(r => new { r.AppUserId, r.Status });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(r => r.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(r => r.IssuedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
