using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class PlatformAdminServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task ListAllReportsAsync_ReturnsReports_AcrossAllCommunities()
    {
        var dbContext = CreateDbContext();
        dbContext.Reports.AddRange(
            new Report { Id = Guid.NewGuid(), ReporterId = Guid.NewGuid(), TargetType = ReportTargetType.Post, TargetId = Guid.NewGuid(), Reason = "r1", Status = ReportStatus.Open, CreatedAtUtc = DateTimeOffset.UtcNow },
            new Report { Id = Guid.NewGuid(), ReporterId = Guid.NewGuid(), TargetType = ReportTargetType.Comment, TargetId = Guid.NewGuid(), Reason = "r2", Status = ReportStatus.Dismissed, CreatedAtUtc = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync();
        var service = new PlatformAdminService(dbContext);

        var (items, totalCount) = await service.ListAllReportsAsync(null, 1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task ListAllReportsAsync_FiltersByStatus()
    {
        var dbContext = CreateDbContext();
        dbContext.Reports.AddRange(
            new Report { Id = Guid.NewGuid(), ReporterId = Guid.NewGuid(), TargetType = ReportTargetType.Post, TargetId = Guid.NewGuid(), Reason = "r1", Status = ReportStatus.Open, CreatedAtUtc = DateTimeOffset.UtcNow },
            new Report { Id = Guid.NewGuid(), ReporterId = Guid.NewGuid(), TargetType = ReportTargetType.Comment, TargetId = Guid.NewGuid(), Reason = "r2", Status = ReportStatus.Dismissed, CreatedAtUtc = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync();
        var service = new PlatformAdminService(dbContext);

        var (items, totalCount) = await service.ListAllReportsAsync(ReportStatus.Open, 1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal(ReportStatus.Open, Assert.Single(items).Status);
    }

    [Fact]
    public async Task ListSpamFlagsAsync_FiltersByStatus_AndOrdersNewestFirst()
    {
        var dbContext = CreateDbContext();
        var older = new SpamFlag { Id = Guid.NewGuid(), TargetType = SpamFlagTargetType.Post, TargetId = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Reason = SpamFlagReason.LinkSpam, Status = SpamFlagStatus.Open, ReportId = Guid.NewGuid(), CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1) };
        var newer = new SpamFlag { Id = Guid.NewGuid(), TargetType = SpamFlagTargetType.Comment, TargetId = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Reason = SpamFlagReason.DuplicateContent, Status = SpamFlagStatus.Open, ReportId = Guid.NewGuid(), CreatedAtUtc = DateTimeOffset.UtcNow };
        var dismissed = new SpamFlag { Id = Guid.NewGuid(), TargetType = SpamFlagTargetType.Post, TargetId = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Reason = SpamFlagReason.RateLimitExceeded, Status = SpamFlagStatus.Dismissed, ReportId = Guid.NewGuid(), CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.SpamFlags.AddRange(older, newer, dismissed);
        await dbContext.SaveChangesAsync();
        var service = new PlatformAdminService(dbContext);

        var (items, totalCount) = await service.ListSpamFlagsAsync(SpamFlagStatus.Open, 1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal([newer.Id, older.Id], items.Select(f => f.Id));
    }

    [Fact]
    public async Task ListAuditLogAsync_ReturnsAllModerationActions_NewestFirst()
    {
        var dbContext = CreateDbContext();
        var older = new ModerationAction { Id = Guid.NewGuid(), ModId = Guid.NewGuid(), ActionType = ModerationActionType.Pin, TargetId = Guid.NewGuid(), CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1) };
        var newer = new ModerationAction { Id = Guid.NewGuid(), ModId = Guid.NewGuid(), ActionType = ModerationActionType.PlatformSuspend, TargetId = Guid.NewGuid(), CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.ModerationActions.AddRange(older, newer);
        await dbContext.SaveChangesAsync();
        var service = new PlatformAdminService(dbContext);

        var (items, totalCount) = await service.ListAuditLogAsync(1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal([newer.Id, older.Id], items.Select(a => a.Id));
    }
}
