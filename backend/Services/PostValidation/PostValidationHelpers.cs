using AnonyMeow.Dtos.Posts;

namespace AnonyMeow.Services.PostValidation;

// Shared between CreatePostRequest validation (PostRequestValidator) and the lighter checks
// PostEndpoints runs directly against UpdatePostRequest, which doesn't carry a Title.
internal static class PostValidationHelpers
{
    public const int MaxTitleLength = 300;
    public const int MaxBodyLength = 40_000;

    public static void RequireTitle(CreatePostRequest request, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Length > MaxTitleLength)
        {
            errors["title"] = [$"Title cannot exceed {MaxTitleLength} characters."];
        }
    }

    // Update path: Title is optional on the request (only patched when the author actually
    // changes it), but when present it must satisfy the same rule creation does.
    public static void ValidateTitleIfProvided(string? title, IDictionary<string, string[]> errors)
    {
        if (title is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (title.Length > MaxTitleLength)
        {
            errors["title"] = [$"Title cannot exceed {MaxTitleLength} characters."];
        }
    }

    public static void ValidateBodyLength(string? bodyMarkdown, IDictionary<string, string[]> errors)
    {
        if (bodyMarkdown is { Length: > MaxBodyLength })
        {
            errors["body"] = [$"Body cannot exceed {MaxBodyLength} characters."];
        }
    }
}
