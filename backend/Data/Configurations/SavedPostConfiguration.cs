using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class SavedPostConfiguration : IEntityTypeConfiguration<SavedPost>
{
    public void Configure(EntityTypeBuilder<SavedPost> builder)
    {
        builder.HasKey(s => new { s.AppUserId, s.PostId });
        builder.Property(s => s.SavedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(s => s.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
