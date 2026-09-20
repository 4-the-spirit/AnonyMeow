using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;

namespace AnonyMeow.Services.SpamDetection;

public interface ISpamFlaggingService
{
    // Runs after the target has already been created/persisted (spam heuristics need its id to
    // file a Report against it, unlike PII detection which blocks before any write). No-op if no
    // heuristic matches.
    Task FlagIfSpamAsync(
        SpamFlagTargetType targetType, Guid targetId, Guid authorId, ContentSubmissionType contentType, string text,
        CancellationToken cancellationToken = default);
}
