using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.SpamDetection;

public interface ISpamHeuristic
{
    SpamFlagReason Reason { get; }

    Task<bool> IsMatchAsync(SpamDetectionContext context, CancellationToken cancellationToken = default);
}
