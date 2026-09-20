using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired();

        builder.HasIndex(i => new { i.PostId, i.Position });

        builder.HasOne<Post>()
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
