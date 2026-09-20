using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

public interface IReportService
{
    // Open to any authenticated user — no authorization beyond "is a completed-profile user."
    Task<Report> CreateAsync(
        ReportTargetType targetType, Guid targetId, Guid reporterId, ReportReasonCategory category, string? details,
        CancellationToken cancellationToken = default);

    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Resolves which community a report's target belongs to (via Post, or Comment -> Post),
    // used both for the CommunityModerator authorization check and to scope the mod queue.
    Task<Guid?> GetCommunityIdForReportAsync(Report report, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Report> Items, int TotalCount)> ListForCommunityAsync(
        Guid communityId, ReportStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    // Sets Status/ReviewedByModId; if outcome is ActionTaken, also writes a ReportResolve audit
    // row via IModerationActionService (the concrete remediation — pin/lock/remove/ban — is a
    // separate, explicit moderator action, kept apart per SRP).
    Task ResolveAsync(
        Report report, Guid communityId, Guid modId, ReportOutcome outcome, string? actionReason,
        CancellationToken cancellationToken = default);
}
