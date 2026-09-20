namespace AnonyMeow.Services.CommentValidation;

// Comments previously had zero server-side validation — CreateCommentAsync/UpdateCommentAsync
// passed BodyMarkdown straight to CommentService. This closes that gap the same way
// PostRequestValidator does for posts.
public class CommentRequestValidator : ICommentRequestValidator
{
    public const int MaxBodyLength = 10_000;

    public IDictionary<string, string[]> Validate(string bodyMarkdown)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(bodyMarkdown))
        {
            errors["bodyMarkdown"] = ["Comment body is required."];
        }
        else if (bodyMarkdown.Length > MaxBodyLength)
        {
            errors["bodyMarkdown"] = [$"Comment cannot exceed {MaxBodyLength} characters."];
        }

        return errors;
    }
}
