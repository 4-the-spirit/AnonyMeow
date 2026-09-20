namespace AnonyMeow.Domain;

public class SavedComment
{
    public Guid AppUserId { get; set; }
    public Guid CommentId { get; set; }
    public DateTimeOffset SavedAtUtc { get; set; }
}
