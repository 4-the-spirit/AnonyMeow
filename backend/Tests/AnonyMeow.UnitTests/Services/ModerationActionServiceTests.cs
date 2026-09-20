using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class ModerationActionServiceTests
{
    private class FakeNotificationDispatcher : INotificationDispatcher
    {
        public List<(Guid RecipientId, NotificationType Type, NotificationSourceType SourceType, Guid SourceId, string PreviewText)> Calls { get; } = [];

        public Task DispatchAsync(
            Guid recipientId, NotificationType type, NotificationSourceType sourceType, Guid sourceId, string previewText,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((recipientId, type, sourceType, sourceId, previewText));
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser Mod, AppUser Author, Community Community, Post Post)> SeedAsync(AppDbContext dbContext)
    {
        var mod = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "mod", CreatedAtUtc = DateTimeOffset.UtcNow };
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "author", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = mod.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var post = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = author.Id, Title = "T", BodyMarkdown = "B", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(mod, author);
        dbContext.Communities.Add(community);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();
        return (mod, author, community, post);
    }

    private static async Task AddMembershipAsync(
        AppDbContext dbContext, Guid communityId, Guid userId, CommunityRole role)
    {
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = communityId,
            AppUserId = userId,
            Role = role,
            JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task PinAsync_SetsIsPinned_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, _, _, post) = await SeedAsync(dbContext);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await service.PinAsync(post, mod.Id, "looks important", CancellationToken.None);

        Assert.True(post.IsPinned);
        var audit = await dbContext.ModerationActions.SingleAsync(a => a.TargetId == post.Id);
        Assert.Equal(ModerationActionType.Pin, audit.ActionType);
        Assert.Equal(mod.Id, audit.ModId);
    }

    [Fact]
    public async Task LockAsync_SetsIsLocked_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, _, _, post) = await SeedAsync(dbContext);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await service.LockAsync(post, mod.Id, null, CancellationToken.None);

        Assert.True(post.IsLocked);
        Assert.Equal(ModerationActionType.Lock, (await dbContext.ModerationActions.SingleAsync()).ActionType);
    }

    [Fact]
    public async Task RemoveAsync_SetsIsRemoved_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, author, _, post) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);

        await service.RemoveAsync(post, mod.Id, "rule violation", CancellationToken.None);

        Assert.True(post.IsRemoved);
        var audit = await dbContext.ModerationActions.SingleAsync();
        Assert.Equal(ModerationActionType.Remove, audit.ActionType);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(author.Id, call.RecipientId);
        Assert.Equal(NotificationType.ModAction, call.Type);
        Assert.Equal(NotificationSourceType.ModerationAction, call.SourceType);
        Assert.Equal(audit.Id, call.SourceId);
    }

    [Fact]
    public async Task BanAsync_CreatesBanRow_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, _) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);

        await service.BanAsync(community, author.Id, mod.Id, "spamming", CancellationToken.None);

        var ban = await dbContext.CommunityBans.SingleAsync(b => b.CommunityId == community.Id && b.AppUserId == author.Id);
        Assert.Equal("spamming", ban.Reason);
        var audit = await dbContext.ModerationActions.SingleAsync();
        Assert.Equal(ModerationActionType.Ban, audit.ActionType);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(author.Id, call.RecipientId);
        Assert.Equal(NotificationType.ModAction, call.Type);
        Assert.Equal(NotificationSourceType.ModerationAction, call.SourceType);
        Assert.Equal(audit.Id, call.SourceId);
    }

    [Fact]
    public async Task UnbanAsync_RemovesBanRow_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, _) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);
        await service.BanAsync(community, author.Id, mod.Id, "spamming", CancellationToken.None);

        await service.UnbanAsync(community, author.Id, mod.Id, CancellationToken.None);

        Assert.False(await dbContext.CommunityBans.AnyAsync(b => b.CommunityId == community.Id && b.AppUserId == author.Id));
        var unbanAudit = await dbContext.ModerationActions
            .Where(a => a.ActionType == ModerationActionType.Unban).SingleAsync();
        Assert.Equal(ModerationActionType.Unban, unbanAudit.ActionType);

        var unbanCall = dispatcher.Calls.Single(c => c.Type == NotificationType.ModAction && c.SourceId == unbanAudit.Id);
        Assert.Equal(author.Id, unbanCall.RecipientId);
        Assert.Equal(NotificationSourceType.ModerationAction, unbanCall.SourceType);
    }

    [Fact]
    public async Task PromoteToModeratorAsync_SetsRoleToModerator_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, _) = await SeedAsync(dbContext);
        await AddMembershipAsync(dbContext, community.Id, author.Id, CommunityRole.Member);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);

        await service.PromoteToModeratorAsync(community, author.Id, mod.Id, CancellationToken.None);

        var membership = await dbContext.CommunityMemberships.SingleAsync(
            m => m.CommunityId == community.Id && m.AppUserId == author.Id);
        Assert.Equal(CommunityRole.Moderator, membership.Role);
        var audit = await dbContext.ModerationActions.SingleAsync();
        Assert.Equal(ModerationActionType.ModeratorPromoted, audit.ActionType);
        Assert.Equal(author.Id, audit.TargetId);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(author.Id, call.RecipientId);
        Assert.Equal(NotificationType.ModAction, call.Type);
    }

    [Fact]
    public async Task PromoteToModeratorAsync_Throws_WhenTargetIsNotAMember()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, _) = await SeedAsync(dbContext);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await Assert.ThrowsAsync<CommunityMemberNotFoundException>(
            () => service.PromoteToModeratorAsync(community, author.Id, mod.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DemoteModeratorAsync_SetsRoleToMember_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, _) = await SeedAsync(dbContext);
        await AddMembershipAsync(dbContext, community.Id, mod.Id, CommunityRole.Moderator);
        await AddMembershipAsync(dbContext, community.Id, author.Id, CommunityRole.Moderator);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);

        await service.DemoteModeratorAsync(community, author.Id, mod.Id, CancellationToken.None);

        var membership = await dbContext.CommunityMemberships.SingleAsync(
            m => m.CommunityId == community.Id && m.AppUserId == author.Id);
        Assert.Equal(CommunityRole.Member, membership.Role);
        var audit = await dbContext.ModerationActions.SingleAsync();
        Assert.Equal(ModerationActionType.ModeratorDemoted, audit.ActionType);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(author.Id, call.RecipientId);
        Assert.Equal(NotificationType.ModAction, call.Type);
    }

    [Fact]
    public async Task DemoteModeratorAsync_Throws_WhenTargetIsTheSoleModerator()
    {
        var dbContext = CreateDbContext();
        var (mod, _, community, _) = await SeedAsync(dbContext);
        await AddMembershipAsync(dbContext, community.Id, mod.Id, CommunityRole.Moderator);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await Assert.ThrowsAsync<SoleModeratorDemoteException>(
            () => service.DemoteModeratorAsync(community, mod.Id, mod.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DemoteModeratorAsync_Throws_WhenTargetIsNotAModerator()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, _) = await SeedAsync(dbContext);
        await AddMembershipAsync(dbContext, community.Id, mod.Id, CommunityRole.Moderator);
        await AddMembershipAsync(dbContext, community.Id, author.Id, CommunityRole.Member);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await Assert.ThrowsAsync<CommunityMemberNotFoundException>(
            () => service.DemoteModeratorAsync(community, author.Id, mod.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveCommentAsync_SetsIsRemoved_AndWritesAuditRow_AndNotifiesAuthor()
    {
        var dbContext = CreateDbContext();
        var (mod, author, community, post) = await SeedAsync(dbContext);
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            AuthorId = author.Id,
            BodyMarkdown = "body",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);

        await service.RemoveCommentAsync(comment, community.Id, mod.Id, "rule violation", CancellationToken.None);

        Assert.True(comment.IsRemoved);
        var audit = await dbContext.ModerationActions.SingleAsync();
        Assert.Equal(ModerationActionType.Remove, audit.ActionType);
        Assert.Equal(comment.Id, audit.TargetId);
        Assert.Equal(community.Id, audit.CommunityId);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(author.Id, call.RecipientId);
        Assert.Equal(NotificationType.ModAction, call.Type);
        Assert.Equal(NotificationSourceType.ModerationAction, call.SourceType);
        Assert.Equal(audit.Id, call.SourceId);
    }

    [Fact]
    public async Task RecordReportResolutionAsync_WritesReportResolveAuditRow()
    {
        var dbContext = CreateDbContext();
        var (mod, _, community, _) = await SeedAsync(dbContext);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());
        var reportId = Guid.NewGuid();

        await service.RecordReportResolutionAsync(community.Id, mod.Id, reportId, "handled", CancellationToken.None);

        var audit = await dbContext.ModerationActions.SingleAsync(a => a.TargetId == reportId);
        Assert.Equal(ModerationActionType.ReportResolve, audit.ActionType);
    }

    [Fact]
    public async Task RestrictAsync_CreatesActiveRestriction_AndWritesPlatformRestrictAuditRow()
    {
        var dbContext = CreateDbContext();
        var (admin, author, _, _) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);

        var restriction = await service.RestrictAsync(
            author.Id, admin.Id, PlatformRestrictionType.PostingRestricted, "spamming", null, CancellationToken.None);

        Assert.Equal(PlatformRestrictionStatus.Active, restriction.Status);
        Assert.Equal(author.Id, restriction.AppUserId);
        Assert.Equal(admin.Id, restriction.IssuedByAdminId);
        var audit = await dbContext.ModerationActions.SingleAsync(a => a.TargetId == author.Id);
        Assert.Equal(ModerationActionType.PlatformRestrict, audit.ActionType);
        Assert.Null(audit.CommunityId);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(author.Id, call.RecipientId);
        Assert.Equal(NotificationType.ModAction, call.Type);
    }

    [Fact]
    public async Task RestrictAsync_FullSuspension_WritesPlatformSuspendAuditRow()
    {
        var dbContext = CreateDbContext();
        var (admin, author, _, _) = await SeedAsync(dbContext);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await service.RestrictAsync(
            author.Id, admin.Id, PlatformRestrictionType.FullSuspension, "abuse", null, CancellationToken.None);

        var audit = await dbContext.ModerationActions.SingleAsync(a => a.TargetId == author.Id);
        Assert.Equal(ModerationActionType.PlatformSuspend, audit.ActionType);
    }

    [Fact]
    public async Task LiftRestrictionAsync_SetsStatusLifted_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (admin, author, _, _) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = new ModerationActionService(dbContext, dispatcher);
        var restriction = await service.RestrictAsync(
            author.Id, admin.Id, PlatformRestrictionType.PostingRestricted, "spamming", null, CancellationToken.None);

        await service.LiftRestrictionAsync(author.Id, admin.Id, CancellationToken.None);

        Assert.Equal(PlatformRestrictionStatus.Lifted, restriction.Status);
        var liftAudit = await dbContext.ModerationActions
            .Where(a => a.ActionType == ModerationActionType.PlatformRestrictionLifted).SingleAsync();
        Assert.Equal(author.Id, liftAudit.TargetId);
    }

    [Fact]
    public async Task LiftRestrictionAsync_NoActiveRestriction_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (admin, author, _, _) = await SeedAsync(dbContext);
        var service = new ModerationActionService(dbContext, new FakeNotificationDispatcher());

        await service.LiftRestrictionAsync(author.Id, admin.Id, CancellationToken.None);

        Assert.Equal(0, await dbContext.ModerationActions.CountAsync());
    }
}
