using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;

namespace AnonyMeow.Common.Options;

// Bound as IOptionsMonitor (hot-reloadable), unlike every other Options class in this codebase
// (plain Configure<T>()) — the operational point of hot-reload here is that ops can demote a
// noisy heuristic detector from Block to LogOnly (or back) via config without a redeploy if false
// positives spike.
public class PiiDetectionOptions
{
    public const string SectionName = "PiiDetection";

    public Dictionary<PiiDetectorType, PiiEnforcementMode> DetectorModes { get; set; } = new()
    {
        [PiiDetectorType.EmailPattern] = PiiEnforcementMode.Block,
        [PiiDetectorType.PhoneNumber] = PiiEnforcementMode.Block,
        // More false-positive-prone than the pattern-based detectors — start conservative
        // (logged, not blocked) per the plan's "concrete thresholds start conservative, iterate".
        [PiiDetectorType.AddressHeuristic] = PiiEnforcementMode.LogOnly
    };

    public HashSet<ContentSubmissionType> AppliesTo { get; set; } =
        [ContentSubmissionType.Post, ContentSubmissionType.Comment, ContentSubmissionType.DirectMessage];
}
