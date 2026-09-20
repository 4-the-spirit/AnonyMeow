using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.PiiDetection;

public interface IPiiDetector
{
    PiiDetectorType DetectorType { get; }

    // Human-facing label surfaced in the 422 response's detectedCategories list.
    string Category { get; }

    bool IsMatch(string text);
}
