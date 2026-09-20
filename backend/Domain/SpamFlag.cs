using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class SpamFlag
{
    public Guid Id { get; set; }
    public SpamFlagTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid AuthorId { get; set; }
    public SpamFlagReason Reason { get; set; }
    public SpamFlagStatus Status { get; set; }
    public Guid ReportId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
