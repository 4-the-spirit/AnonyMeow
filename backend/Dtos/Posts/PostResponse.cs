using AnonyMeow.Domain;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Reactions;

namespace AnonyMeow.Dtos.Posts;

public record PostResponse(
    Guid Id,
    string CommunityName,
    string AuthorUsername,
    string? AuthorDisplayName,
    string Title,
    string? BodyMarkdown,
    string? Url,
    IReadOnlyList<string> ImageUrls,
    int Score,
    int CommentCount,
    bool IsPinned,
    bool IsLocked,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EditedAtUtc,
    IReadOnlyList<PollOptionResponse>? PollOptions = null,
    FlairResponse? Flair = null,
    IReadOnlyList<ReactionSummaryResponse>? Reactions = null,
    string? AuthorAvatarSeed = null,
    // The viewer's own +1/-1 vote on this post; null if they haven't voted (or aren't signed in).
    sbyte? ViewerVote = null)
{
    // PollOptions is populated when the post has a poll (on detail/create responses only, not
    // list responses) so a client has poll option ids to vote with, without paying an extra
    // query per item in a feed listing.
    public static PostResponse FromEntity(
        Post post, string communityName, string authorUsername, IReadOnlyList<string> imageUrls,
        int score, int commentCount,
        IReadOnlyList<PollOptionResponse>? pollOptions = null, FlairResponse? flair = null,
        IReadOnlyList<ReactionSummaryResponse>? reactions = null, string? authorAvatarSeed = null,
        string? authorDisplayName = null, sbyte? viewerVote = null) => new(
        post.Id,
        communityName,
        authorUsername,
        authorDisplayName,
        post.Title,
        post.BodyMarkdown,
        post.Url,
        imageUrls,
        score,
        commentCount,
        post.IsPinned,
        post.IsLocked,
        post.CreatedAtUtc,
        post.EditedAtUtc,
        pollOptions,
        flair,
        reactions ?? [],
        authorAvatarSeed,
        viewerVote);
}
