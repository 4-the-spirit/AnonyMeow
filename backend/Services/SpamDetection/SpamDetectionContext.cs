using AnonyMeow.Services.ContentSubmission;

namespace AnonyMeow.Services.SpamDetection;

public record SpamDetectionContext(Guid AuthorId, ContentSubmissionType ContentType, string Text);
