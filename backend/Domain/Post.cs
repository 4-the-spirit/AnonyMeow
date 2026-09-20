using NpgsqlTypes;

namespace AnonyMeow.Domain;

public class Post
{
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? FlairId { get; set; }
    public required string Title { get; set; }
    public string? BodyMarkdown { get; set; }
    public string? Url { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public bool IsRemoved { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EditedAtUtc { get; set; }

    public ICollection<PostImage> Images { get; set; } = new List<PostImage>();

    // Postgres generated column (Title + BodyMarkdown) — never set from code, see PostConfiguration.
    public NpgsqlTsVector? SearchVector { get; set; }
}
