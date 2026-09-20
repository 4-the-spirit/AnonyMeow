using AnonyMeow.Common.Options;
using AnonyMeow.Services.ContentSubmission;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services.PiiDetection;

public class CompositePiiDetectionService(
    IEnumerable<IPiiDetector> detectors, IOptionsMonitor<PiiDetectionOptions> optionsMonitor) : IPiiDetectionService
{
    public IReadOnlyList<PiiDetectionMatch> Detect(string text, ContentSubmissionType contentType)
    {
        // Read per call, not cached at construction — required for the hot-reload behavior
        // (ops demoting a noisy detector from Block to LogOnly without a redeploy) to take effect.
        var options = optionsMonitor.CurrentValue;
        if (!options.AppliesTo.Contains(contentType))
        {
            return [];
        }

        var matches = new List<PiiDetectionMatch>();
        foreach (var detector in detectors)
        {
            var mode = options.DetectorModes.GetValueOrDefault(detector.DetectorType, PiiEnforcementMode.Block);
            if (mode == PiiEnforcementMode.Disabled)
            {
                continue;
            }

            if (detector.IsMatch(text))
            {
                matches.Add(new PiiDetectionMatch(detector.DetectorType, detector.Category, mode == PiiEnforcementMode.Block));
            }
        }

        return matches;
    }
}
