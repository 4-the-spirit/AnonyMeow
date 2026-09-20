using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Services.PiiDetection;

namespace AnonyMeow.Services.ContentSubmission;

public class ContentSubmissionPipeline(IPiiDetectionService piiDetectionService, AppDbContext dbContext) : IContentSubmissionPipeline
{
    public async Task<ContentSubmissionResult> EvaluateAsync(
        ContentSubmissionRequest submission, CancellationToken cancellationToken = default)
    {
        var matches = piiDetectionService.Detect(submission.Text, submission.ContentType);
        if (matches.Count == 0)
        {
            return ContentSubmissionResult.Allowed;
        }

        foreach (var match in matches)
        {
            dbContext.PiiDetectionLogs.Add(new PiiDetectionLog
            {
                Id = Guid.NewGuid(),
                TargetType = submission.ContentType,
                UserId = submission.AuthorId,
                DetectorType = match.DetectorType,
                MatchedCategory = match.MatchedCategory,
                WasBlocked = match.WasBlocked,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var blockedCategories = matches.Where(m => m.WasBlocked).Select(m => m.MatchedCategory).Distinct().ToList();
        return new ContentSubmissionResult(blockedCategories.Count > 0, blockedCategories);
    }
}
