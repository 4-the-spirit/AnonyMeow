using NpgsqlTypes;

namespace AnonyMeow.Domain;

public class Community
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string IconImageUrl { get; set; }
    public required string BannerImageUrl { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    // Postgres generated column (Name + Description) — never set from code, see CommunityConfiguration.
    public NpgsqlTsVector? SearchVector { get; set; }
}
