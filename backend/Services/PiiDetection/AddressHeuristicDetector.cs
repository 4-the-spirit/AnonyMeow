using System.Text.RegularExpressions;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.PiiDetection;

public partial class AddressHeuristicDetector : IPiiDetector
{
    public PiiDetectorType DetectorType => PiiDetectorType.AddressHeuristic;
    public string Category => "Street Address";

    // Heuristic, not exhaustive: a leading number followed by 1-4 words and a common street
    // suffix — e.g. "123 Main St", "42 Oak Avenue". More false-positive-prone than the
    // pattern-based detectors above, hence defaulting to LogOnly in PiiDetectionOptions.
    [GeneratedRegex(
        @"\b\d{1,6}\s+([A-Za-z]+\s?){1,4}(Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Lane|Ln|Drive|Dr|Court|Ct|Way|Place|Pl)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();

    public bool IsMatch(string text) => AddressPattern().IsMatch(text);
}
