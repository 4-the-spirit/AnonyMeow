using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.PiiDetection;

public record PiiDetectionMatch(PiiDetectorType DetectorType, string MatchedCategory, bool WasBlocked);
