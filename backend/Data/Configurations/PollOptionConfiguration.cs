using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonyMeow.Data.Configurations;

public class PollOptionConfiguration : IEntityTypeConfiguration<PollOption>
{
    public void Configure(EntityTypeBuilder<PollOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Text).IsRequired();

        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(o => o.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
