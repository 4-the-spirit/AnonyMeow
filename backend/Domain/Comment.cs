using NpgsqlTypes;

namespace AnonyMeow.Domain;

public class Comment
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public Guid AuthorId { get; set; }
    public required string BodyMarkdown { get; set; }
    public bool IsRemoved { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EditedAtUtc { get; set; }

    // Postgres generated column (BodyMarkdown) — never set from code, see CommentConfiguration.
    public NpgsqlTsVector? SearchVector { get; set; }
}
