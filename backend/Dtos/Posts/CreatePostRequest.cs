namespace AnonyMeow.Dtos.Posts;

public record CreatePostRequest(
    string Title,
    string? BodyMarkdown,
    string? Url,
    IReadOnlyList<string>? ImageUrls,
    IReadOnlyList<string>? PollOptions,
    Guid FlairId);
