using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class FlairConfiguration : IEntityTypeConfiguration<Flair>
{
    public void Configure(EntityTypeBuilder<Flair> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired();
        builder.Property(f => f.ColorHex).IsRequired();
        builder.Property(f => f.CreatedAtUtc).HasDefaultValueSql("now()");

        builder.HasIndex(f => new { f.CommunityId, f.Name }).IsUnique();

        builder.HasOne<Community>()
            .WithMany()
            .HasForeignKey(f => f.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(f => f.CreatedByModId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
