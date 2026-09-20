namespace AnonyMeow.Domain;

public class Flair
{
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; }
    public required string Name { get; set; }
    public required string ColorHex { get; set; }
    public bool IsDefault { get; set; }
    public Guid CreatedByModId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
