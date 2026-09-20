namespace AnonyMeow.Domain;

public class CommunityRule
{
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    // Preserves the creator/moderator's chosen display order (a plain 0-based position, not a
    // sort key with gaps) since rules are edited as a whole list, not individually reordered.
    public int Order { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
