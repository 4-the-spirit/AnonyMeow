using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

public interface IPlatformAdminService
{
    // Platform-wide report queue — same Reports table Phase 1.6's community-scoped queue reads,
    // just without the community filter.
    Task<(IReadOnlyList<Report> Items, int TotalCount)> ListAllReportsAsync(
        ReportStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SpamFlag> Items, int TotalCount)> ListSpamFlagsAsync(
        SpamFlagStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    // Reuses ModerationAction — one unified audit trail across community and platform actions.
    Task<(IReadOnlyList<ModerationAction> Items, int TotalCount)> ListAuditLogAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);
}
