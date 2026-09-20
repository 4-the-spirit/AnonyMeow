using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.SpamDetection;

public interface ISpamDetectionService
{
    Task<IReadOnlyList<SpamFlagReason>> DetectAsync(SpamDetectionContext context, CancellationToken cancellationToken = default);
}
