using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class ReportServiceTests
{
    private class NoOpDispatcher : INotificationDispatcher
    {
        public Task DispatchAsync(
            Guid recipientId, NotificationType type, NotificationSourceType sourceType, Guid sourceId, string previewText,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser Reporter, AppUser Mod, Community Community, Post Post)> SeedAsync(AppDbContext dbContext)
    {
        var reporter = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "reporter", CreatedAtUtc = DateTimeOffset.UtcNow };
        var mod = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "mod", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = mod.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var post = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = mod.Id, Title = "T", BodyMarkdown = "B", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(reporter, mod);
        dbContext.Communities.Add(community);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();
        return (reporter, mod, community, post);
    }

    [Fact]
    public async Task CreateAsync_CreatesOpenReport()
    {
        var dbContext = CreateDbContext();
        var (reporter, _, _, post) = await SeedAsync(dbContext);
        var service = new ReportService(dbContext, new ModerationActionService(dbContext, new NoOpDispatcher()));

        var report = await service.CreateAsync(ReportTargetType.Post, post.Id, reporter.Id, ReportReasonCategory.Spam, "spam", CancellationToken.None);

        Assert.Equal(ReportStatus.Open, report.Status);
        Assert.Equal(ReportReasonCategory.Spam, report.Category);
        Assert.Equal("spam", report.Reason);
    }

    [Fact]
    public async Task ResolveAsync_Dismiss_SetsDismissedStatus_AndWritesNoAuditRow()
    {
        var dbContext = CreateDbContext();
        var (reporter, mod, community, post) = await SeedAsync(dbContext);
        var service = new ReportService(dbContext, new ModerationActionService(dbContext, new NoOpDispatcher()));
        var report = await service.CreateAsync(ReportTargetType.Post, post.Id, reporter.Id, ReportReasonCategory.Spam, "spam", CancellationToken.None);

        await service.ResolveAsync(report, community.Id, mod.Id, ReportOutcome.Dismiss, null, CancellationToken.None);

        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal(mod.Id, report.ReviewedByModId);
        Assert.False(await dbContext.ModerationActions.AnyAsync());
    }

    [Fact]
    public async Task ResolveAsync_ActionTaken_SetsActionTakenStatus_AndWritesAuditRow()
    {
        var dbContext = CreateDbContext();
        var (reporter, mod, community, post) = await SeedAsync(dbContext);
        var service = new ReportService(dbContext, new ModerationActionService(dbContext, new NoOpDispatcher()));
        var report = await service.CreateAsync(ReportTargetType.Post, post.Id, reporter.Id, ReportReasonCategory.Spam, "spam", CancellationToken.None);

        await service.ResolveAsync(report, community.Id, mod.Id, ReportOutcome.ActionTaken, "removed the post", CancellationToken.None);

        Assert.Equal(ReportStatus.ActionTaken, report.Status);
        var audit = await dbContext.ModerationActions.SingleAsync(a => a.TargetId == report.Id);
        Assert.Equal(ModerationActionType.ReportResolve, audit.ActionType);
    }

    [Fact]
    public async Task GetCommunityIdForReportAsync_ResolvesViaPost()
    {
        var dbContext = CreateDbContext();
        var (reporter, _, community, post) = await SeedAsync(dbContext);
        var service = new ReportService(dbContext, new ModerationActionService(dbContext, new NoOpDispatcher()));
        var report = await service.CreateAsync(ReportTargetType.Post, post.Id, reporter.Id, ReportReasonCategory.Spam, "spam", CancellationToken.None);

        var communityId = await service.GetCommunityIdForReportAsync(report, CancellationToken.None);

        Assert.Equal(community.Id, communityId);
    }
}
