using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Admin;

public record SpamFlagResponse(
    Guid Id, SpamFlagTargetType TargetType, Guid TargetId, Guid AuthorId, SpamFlagReason Reason,
    SpamFlagStatus Status, Guid ReportId, DateTimeOffset CreatedAtUtc)
{
    public static SpamFlagResponse FromEntity(SpamFlag flag) =>
        new(flag.Id, flag.TargetType, flag.TargetId, flag.AuthorId, flag.Reason, flag.Status, flag.ReportId, flag.CreatedAtUtc);
}
