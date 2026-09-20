namespace AnonyMeow.Services.ContentSubmission;

public record ContentSubmissionResult(bool IsBlocked, IReadOnlyList<string> DetectedCategories)
{
    public static ContentSubmissionResult Allowed { get; } = new(false, []);
}
