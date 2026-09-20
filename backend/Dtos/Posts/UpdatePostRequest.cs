namespace AnonyMeow.Dtos.Posts;

public record UpdatePostRequest(string? Title, string? BodyMarkdown, string? Url);
