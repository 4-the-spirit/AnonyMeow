using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class SavedCommentConfiguration : IEntityTypeConfiguration<SavedComment>
{
    public void Configure(EntityTypeBuilder<SavedComment> builder)
    {
        builder.HasKey(s => new { s.AppUserId, s.CommentId });
        builder.Property(s => s.SavedAtUtc).HasDefaultValueSql("now()");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(s => s.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
