using AnonyMeow.Dtos.Reactions;

namespace AnonyMeow.Dtos.Comments;

public record CommentResponse(
    Guid Id,
    string AuthorUsername,
    string? AuthorDisplayName,
    string BodyMarkdown,
    int Score,
    int ReplyCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EditedAtUtc,
    IReadOnlyList<ReactionSummaryResponse>? Reactions = null,
    Guid? PostId = null,
    IReadOnlyList<Guid>? AncestorCommentIds = null,
    string? AuthorAvatarSeed = null,
    string? CommunityName = null,
    // The viewer's own +1/-1 vote on this comment; null if they haven't voted (or aren't signed in).
    sbyte? ViewerVote = null);
