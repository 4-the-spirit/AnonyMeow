using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services.SpamDetection;

public class SpamFlaggingServiceTests
{
    private class FakeNotificationDispatcher : INotificationDispatcher
    {
        public Task DispatchAsync(
            Guid recipientId, NotificationType type, NotificationSourceType sourceType, Guid sourceId, string previewText,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeSpamDetectionService(IReadOnlyList<SpamFlagReason> reasons) : ISpamDetectionService
    {
        public Task<IReadOnlyList<SpamFlagReason>> DetectAsync(
            SpamDetectionContext context, CancellationToken cancellationToken = default) => Task.FromResult(reasons);
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SpamFlaggingService CreateService(AppDbContext dbContext, IReadOnlyList<SpamFlagReason> reasons)
    {
        var reportService = new ReportService(dbContext, new ModerationActionService(dbContext, new FakeNotificationDispatcher()));
        return new SpamFlaggingService(dbContext, new FakeSpamDetectionService(reasons), reportService);
    }

    private static async Task<AppUser> SeedAuthorAsync(AppDbContext dbContext)
    {
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "author", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(author);
        await dbContext.SaveChangesAsync();
        return author;
    }

    [Fact]
    public async Task FlagIfSpamAsync_NoMatches_CreatesNoFlagOrReport()
    {
        var dbContext = CreateDbContext();
        var author = await SeedAuthorAsync(dbContext);
        var service = CreateService(dbContext, []);

        await service.FlagIfSpamAsync(SpamFlagTargetType.Post, Guid.NewGuid(), author.Id, ContentSubmissionType.Post, "clean text");

        Assert.Equal(0, await dbContext.SpamFlags.CountAsync());
        Assert.Equal(0, await dbContext.Reports.CountAsync());
    }

    [Fact]
    public async Task FlagIfSpamAsync_OneMatch_CreatesSpamFlagAndReport_ViaSystemReporter()
    {
        var dbContext = CreateDbContext();
        var author = await SeedAuthorAsync(dbContext);
        var postId = Guid.NewGuid();
        var service = CreateService(dbContext, [SpamFlagReason.LinkSpam]);

        await service.FlagIfSpamAsync(SpamFlagTargetType.Post, postId, author.Id, ContentSubmissionType.Post, "spam text");

        var flag = await dbContext.SpamFlags.SingleAsync();
        Assert.Equal(SpamFlagReason.LinkSpam, flag.Reason);
        Assert.Equal(postId, flag.TargetId);
        Assert.Equal(author.Id, flag.AuthorId);
        Assert.Equal(SpamFlagStatus.Open, flag.Status);

        var report = await dbContext.Reports.SingleAsync();
        Assert.Equal(flag.ReportId, report.Id);
        Assert.Equal(ReportTargetType.Post, report.TargetType);
        Assert.Equal(postId, report.TargetId);
        Assert.NotEqual(author.Id, report.ReporterId);
    }

    [Fact]
    public async Task FlagIfSpamAsync_MultipleReasons_CreatesOneFlagPerReason_ButOneSharedReport()
    {
        var dbContext = CreateDbContext();
        var author = await SeedAuthorAsync(dbContext);
        var service = CreateService(dbContext, [SpamFlagReason.LinkSpam, SpamFlagReason.DuplicateContent]);

        await service.FlagIfSpamAsync(SpamFlagTargetType.Comment, Guid.NewGuid(), author.Id, ContentSubmissionType.Comment, "spam");

        Assert.Equal(2, await dbContext.SpamFlags.CountAsync());
        Assert.Equal(1, await dbContext.Reports.CountAsync());
    }

    [Fact]
    public async Task FlagIfSpamAsync_ReusesSameSystemReporter_AcrossCalls()
    {
        var dbContext = CreateDbContext();
        var author = await SeedAuthorAsync(dbContext);
        var service = CreateService(dbContext, [SpamFlagReason.LinkSpam]);

        await service.FlagIfSpamAsync(SpamFlagTargetType.Post, Guid.NewGuid(), author.Id, ContentSubmissionType.Post, "spam 1");
        await service.FlagIfSpamAsync(SpamFlagTargetType.Post, Guid.NewGuid(), author.Id, ContentSubmissionType.Post, "spam 2");

        var reports = await dbContext.Reports.ToListAsync();
        Assert.Equal(2, reports.Count);
        Assert.Equal(reports[0].ReporterId, reports[1].ReporterId);
        // The system reporter is a real AppUser row (Report.ReporterId has a DB FK) but never
        // completes a profile, so it has no username.
        var systemUser = await dbContext.Users.SingleAsync(u => u.Id == reports[0].ReporterId);
        Assert.Null(systemUser.Username);
    }
}
