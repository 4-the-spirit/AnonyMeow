using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.SpamDetection;

// Composite pattern — every registered heuristic runs, same shape as Phase 6's
// CompositePiiDetectionService.
public class CompositeSpamDetectionService(IEnumerable<ISpamHeuristic> heuristics) : ISpamDetectionService
{
    public async Task<IReadOnlyList<SpamFlagReason>> DetectAsync(
        SpamDetectionContext context, CancellationToken cancellationToken = default)
    {
        var reasons = new List<SpamFlagReason>();
        foreach (var heuristic in heuristics)
        {
            if (await heuristic.IsMatchAsync(context, cancellationToken))
            {
                reasons.Add(heuristic.Reason);
            }
        }

        return reasons;
    }
}
