using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Posts;

namespace AnonyMeow.Dtos.Search;

// One requested "type" (posts/comments/communities) leaves the other two lists empty with a 0
// count, rather than three separately-paginated responses — keeps the endpoint to a single
// round trip for the common "type=all" case.
public record SearchResponse(
    IReadOnlyList<PostResponse> Posts,
    int PostsTotalCount,
    IReadOnlyList<CommentResponse> Comments,
    int CommentsTotalCount,
    IReadOnlyList<CommunityResponse> Communities,
    int CommunitiesTotalCount);
