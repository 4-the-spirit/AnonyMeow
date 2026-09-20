namespace AnonyMeow.Services.CommentValidation;

public interface ICommentRequestValidator
{
    IDictionary<string, string[]> Validate(string bodyMarkdown);
}
