using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class ModerationActionService(AppDbContext dbContext, INotificationDispatcher notificationDispatcher) : IModerationActionService
{
    public async Task PinAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default)
    {
        post.IsPinned = true;
        await RecordActionAsync(post.CommunityId, modId, ModerationActionType.Pin, post.Id, reason, cancellationToken);
    }

    public async Task UnpinAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default)
    {
        post.IsPinned = false;
        await RecordActionAsync(post.CommunityId, modId, ModerationActionType.Unpin, post.Id, reason, cancellationToken);
    }

    public async Task LockAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default)
    {
        post.IsLocked = true;
        await RecordActionAsync(post.CommunityId, modId, ModerationActionType.Lock, post.Id, reason, cancellationToken);
    }

    public async Task UnlockAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default)
    {
        post.IsLocked = false;
        await RecordActionAsync(post.CommunityId, modId, ModerationActionType.Unlock, post.Id, reason, cancellationToken);
    }

    public async Task RemoveAsync(Post post, Guid modId, string? reason, CancellationToken cancellationToken = default)
    {
        post.IsRemoved = true;
        var action = await RecordActionAsync(post.CommunityId, modId, ModerationActionType.Remove, post.Id, reason, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            post.AuthorId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            "Your post was removed by a moderator.", cancellationToken);
    }

    public async Task BanAsync(
        Community community, Guid targetUserId, Guid modId, string reason, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.CommunityBans.FindAsync([community.Id, targetUserId], cancellationToken);
        if (existing is null)
        {
            dbContext.CommunityBans.Add(new CommunityBan
            {
                CommunityId = community.Id,
                AppUserId = targetUserId,
                BannedByModId = modId,
                Reason = reason,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        var action = await RecordActionAsync(community.Id, modId, ModerationActionType.Ban, targetUserId, reason, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            targetUserId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            $"You were banned from {community.Name}.", cancellationToken);
    }

    public async Task UnbanAsync(
        Community community, Guid targetUserId, Guid modId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.CommunityBans.FindAsync([community.Id, targetUserId], cancellationToken);
        if (existing is not null)
        {
            dbContext.CommunityBans.Remove(existing);
        }

        var action = await RecordActionAsync(community.Id, modId, ModerationActionType.Unban, targetUserId, null, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            targetUserId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            $"Your ban from {community.Name} was lifted.", cancellationToken);
    }

    public async Task PromoteToModeratorAsync(
        Community community, Guid targetUserId, Guid modId, CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.CommunityMemberships.SingleOrDefaultAsync(
            m => m.CommunityId == community.Id && m.AppUserId == targetUserId, cancellationToken);
        if (membership is null)
        {
            throw new CommunityMemberNotFoundException();
        }

        membership.Role = CommunityRole.Moderator;
        var action = await RecordActionAsync(
            community.Id, modId, ModerationActionType.ModeratorPromoted, targetUserId, null, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            targetUserId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            $"You were made a moderator of {community.Name}.", cancellationToken);
    }

    public async Task DemoteModeratorAsync(
        Community community, Guid targetUserId, Guid modId, CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.CommunityMemberships.SingleOrDefaultAsync(
            m => m.CommunityId == community.Id && m.AppUserId == targetUserId, cancellationToken);
        if (membership is null || membership.Role != CommunityRole.Moderator)
        {
            throw new CommunityMemberNotFoundException();
        }

        var moderatorCount = await dbContext.CommunityMemberships.CountAsync(
            m => m.CommunityId == community.Id && m.Role == CommunityRole.Moderator, cancellationToken);
        if (moderatorCount <= 1)
        {
            throw new SoleModeratorDemoteException(community.Name);
        }

        membership.Role = CommunityRole.Member;
        var action = await RecordActionAsync(
            community.Id, modId, ModerationActionType.ModeratorDemoted, targetUserId, null, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            targetUserId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            $"You are no longer a moderator of {community.Name}.", cancellationToken);
    }

    public async Task RemoveCommentAsync(
        Comment comment, Guid communityId, Guid modId, string? reason, CancellationToken cancellationToken = default)
    {
        comment.IsRemoved = true;
        var action = await RecordActionAsync(communityId, modId, ModerationActionType.Remove, comment.Id, reason, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            comment.AuthorId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            "Your comment was removed by a moderator.", cancellationToken);
    }

    public async Task RecordReportResolutionAsync(
        Guid communityId, Guid modId, Guid reportId, string? reason, CancellationToken cancellationToken = default) =>
        await RecordActionAsync(communityId, modId, ModerationActionType.ReportResolve, reportId, reason, cancellationToken);

    public async Task<PlatformRestriction> RestrictAsync(
        Guid targetUserId, Guid adminId, PlatformRestrictionType type, string reason, DateTimeOffset? endAtUtc,
        CancellationToken cancellationToken = default)
    {
        var restriction = new PlatformRestriction
        {
            Id = Guid.NewGuid(),
            AppUserId = targetUserId,
            Type = type,
            Reason = reason,
            IssuedByAdminId = adminId,
            StartAtUtc = DateTimeOffset.UtcNow,
            EndAtUtc = endAtUtc,
            Status = PlatformRestrictionStatus.Active
        };
        dbContext.PlatformRestrictions.Add(restriction);

        var actionType = type == PlatformRestrictionType.FullSuspension
            ? ModerationActionType.PlatformSuspend
            : ModerationActionType.PlatformRestrict;
        var action = await RecordActionAsync(null, adminId, actionType, targetUserId, reason, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            targetUserId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            "A platform administrator has placed a restriction on your account.", cancellationToken);

        return restriction;
    }

    public async Task LiftRestrictionAsync(Guid targetUserId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var active = await dbContext.PlatformRestrictions.SingleOrDefaultAsync(
            r => r.AppUserId == targetUserId && r.Status == PlatformRestrictionStatus.Active, cancellationToken);
        if (active is null)
        {
            return;
        }

        active.Status = PlatformRestrictionStatus.Lifted;

        var action = await RecordActionAsync(
            null, adminId, ModerationActionType.PlatformRestrictionLifted, targetUserId, null, cancellationToken);
        await notificationDispatcher.DispatchAsync(
            targetUserId, NotificationType.ModAction, NotificationSourceType.ModerationAction, action.Id,
            "A platform restriction on your account has been lifted.", cancellationToken);
    }

    private async Task<ModerationAction> RecordActionAsync(
        Guid? communityId, Guid modId, ModerationActionType actionType, Guid targetId, string? reason,
        CancellationToken cancellationToken)
    {
        var action = new ModerationAction
        {
            Id = Guid.NewGuid(),
            CommunityId = communityId,
            ModId = modId,
            ActionType = actionType,
            TargetId = targetId,
            Reason = reason,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.ModerationActions.Add(action);

        await dbContext.SaveChangesAsync(cancellationToken);
        return action;
    }
}
