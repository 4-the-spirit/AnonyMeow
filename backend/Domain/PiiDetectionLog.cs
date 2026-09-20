using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;

namespace AnonyMeow.Domain;

public class PiiDetectionLog
{
    public Guid Id { get; set; }
    public ContentSubmissionType TargetType { get; set; }
    public Guid UserId { get; set; }
    public PiiDetectorType DetectorType { get; set; }

    // Redacted category label only (e.g. "Email") — never the raw matched text.
    public required string MatchedCategory { get; set; }
    public bool WasBlocked { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
