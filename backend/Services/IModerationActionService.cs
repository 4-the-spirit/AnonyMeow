using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

// Authorization (CommunityModerator/PlatformAdmin) is checked by the caller (endpoint) before any
// of these are invoked — each method here only performs: state mutation -> audit row ->
// notification hook.
public interface IModerationActionService
{
    Task PinAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default);

    Task UnpinAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default);

    Task LockAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default);

    Task UnlockAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default);

    Task RemoveAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default);

    Task BanAsync(Community community, Guid targetUserId, Guid modId, string reason, CancellationToken cancellationToken = default);

    Task UnbanAsync(Community community, Guid targetUserId, Guid modId, CancellationToken cancellationToken = default);

    // Throws CommunityMemberNotFoundException if the target has no membership row in the community.
    Task PromoteToModeratorAsync(Community community, Guid targetUserId, Guid modId, CancellationToken cancellationToken = default);

    // Throws CommunityMemberNotFoundException if the target isn't currently a moderator, or
    // SoleModeratorDemoteException if they're the community's last moderator.
    Task DemoteModeratorAsync(Community community, Guid targetUserId, Guid modId, CancellationToken cancellationToken = default);

    Task RemoveCommentAsync(Comment comment, Guid communityId, Guid modId, string? reason, CancellationToken cancellationToken = default);

    Task RecordReportResolutionAsync(
        Guid communityId, Guid modId, Guid reportId, string? reason, CancellationToken cancellationToken = default);

    // Platform-level actions (Phase 9) — CommunityId is null in the resulting audit row, same as
    // ReportResolve's community-scoped rows but at platform scope instead.
    Task<PlatformRestriction> RestrictAsync(
        Guid targetUserId, Guid adminId, PlatformRestrictionType type, string reason, DateTimeOffset? endAtUtc,
        CancellationToken cancellationToken = default);

    // No-op if the user has no active restriction.
    Task LiftRestrictionAsync(Guid targetUserId, Guid adminId, CancellationToken cancellationToken = default);
}
