using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Moderation;

public record ReportResponse(
    Guid Id,
    ReportTargetType TargetType,
    Guid TargetId,
    ReportReasonCategory Category,
    string? Details,
    ReportStatus Status,
    DateTimeOffset CreatedAtUtc)
{
    public static ReportResponse FromEntity(Report report) =>
        new(report.Id, report.TargetType, report.TargetId, report.Category, report.Reason, report.Status, report.CreatedAtUtc);
}
