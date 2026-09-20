namespace AnonyMeow.Dtos.Comments;

// Shared by both PostEndpoints and CommentEndpoints' vote routes.
public record CastVoteRequest(sbyte Value);
