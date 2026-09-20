using AnonyMeow.Services.ContentSubmission;

namespace AnonyMeow.Services.PiiDetection;

public interface IPiiDetectionService
{
    IReadOnlyList<PiiDetectionMatch> Detect(string text, ContentSubmissionType contentType);
}
