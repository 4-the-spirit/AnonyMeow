using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.B2CObjectId).IsRequired();
        builder.HasIndex(u => u.B2CObjectId).IsUnique();

        builder.Property(u => u.Username);
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasFilter("\"Username\" IS NOT NULL");

        builder.Property(u => u.CreatedAtUtc).HasDefaultValueSql("now()");
    }
}
