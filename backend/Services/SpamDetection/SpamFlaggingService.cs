using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services.SpamDetection;

// Routes spam-heuristic hits into the existing Report/ModerationAction review workflow via a
// synthetic system reporter, rather than a parallel review workflow — content stays published
// (unlike PII's hard block), just flagged for a moderator, per the parent plan's Phase 8 note.
public class SpamFlaggingService(
    AppDbContext dbContext, ISpamDetectionService spamDetectionService, IReportService reportService)
    : ISpamFlaggingService
{
    // Well-known B2CObjectId anchoring the system account that files automated spam reports — it
    // never logs in via B2C, so it can't collide with a real user's oid. Created lazily on first
    // use rather than via migration seed data (no HasData precedent in this codebase, and a
    // lazily-created row needs no migration to keep in sync with).
    private const string SystemReporterB2CObjectId = "system:spam-detection";

    public async Task FlagIfSpamAsync(
        SpamFlagTargetType targetType, Guid targetId, Guid authorId, ContentSubmissionType contentType, string text,
        CancellationToken cancellationToken = default)
    {
        var reasons = await spamDetectionService.DetectAsync(
            new SpamDetectionContext(authorId, contentType, text), cancellationToken);
        if (reasons.Count == 0)
        {
            return;
        }

        var reporterId = await GetOrCreateSystemReporterIdAsync(cancellationToken);
        var reasonSummary = string.Join(", ", reasons.Distinct());
        var report = await reportService.CreateAsync(
            MapTargetType(targetType), targetId, reporterId, ReportReasonCategory.Spam,
            $"Automated spam detection: {reasonSummary}", cancellationToken);

        foreach (var reason in reasons)
        {
            dbContext.SpamFlags.Add(new SpamFlag
            {
                Id = Guid.NewGuid(),
                TargetType = targetType,
                TargetId = targetId,
                AuthorId = authorId,
                Reason = reason,
                Status = SpamFlagStatus.Open,
                ReportId = report.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ReportTargetType MapTargetType(SpamFlagTargetType targetType) => targetType switch
    {
        SpamFlagTargetType.Post => ReportTargetType.Post,
        SpamFlagTargetType.Comment => ReportTargetType.Comment,
        SpamFlagTargetType.DirectMessage => ReportTargetType.DirectMessage,
        _ => throw new ArgumentOutOfRangeException(nameof(targetType))
    };

    private async Task<Guid> GetOrCreateSystemReporterIdAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users.SingleOrDefaultAsync(
            u => u.B2CObjectId == SystemReporterB2CObjectId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var systemUser = new AppUser
        {
            Id = Guid.NewGuid(),
            B2CObjectId = SystemReporterB2CObjectId,
            DisplayName = "Spam Detection",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(systemUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        return systemUser.Id;
    }
}
