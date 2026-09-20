using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class ModerationAction
{
    public Guid Id { get; set; }
    public Guid? CommunityId { get; set; }
    public Guid ModId { get; set; }
    public ModerationActionType ActionType { get; set; }
    public Guid TargetId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
