namespace AnonyMeow.Domain;

public class SavedPost
{
    public Guid AppUserId { get; set; }
    public Guid PostId { get; set; }
    public DateTimeOffset SavedAtUtc { get; set; }
}
