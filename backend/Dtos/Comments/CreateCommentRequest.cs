namespace AnonyMeow.Dtos.Comments;

public record CreateCommentRequest(string BodyMarkdown, Guid? ParentCommentId);
