using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class Report
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ReportReasonCategory Category { get; set; }
    // Optional free-text elaboration on top of Category — the primary signal used to be a
    // required free-text reason; Category now carries that role instead.
    public string? Reason { get; set; }
    public ReportStatus Status { get; set; }
    public Guid? ReviewedByModId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
