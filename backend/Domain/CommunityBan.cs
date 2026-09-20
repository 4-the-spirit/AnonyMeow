namespace AnonyMeow.Domain;

public class CommunityBan
{
    public Guid CommunityId { get; set; }
    public Guid AppUserId { get; set; }
    public Guid BannedByModId { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
