namespace AnonyMeow.Services.ContentSubmission;

public record ContentSubmissionRequest(ContentSubmissionType ContentType, Guid AuthorId, string Text);
