using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class Reaction
{
    public Guid Id { get; set; }
    public ReactionTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid AppUserId { get; set; }
    public required string Emoji { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
