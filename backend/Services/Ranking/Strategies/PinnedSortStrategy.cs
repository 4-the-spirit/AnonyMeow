using AnonyMeow.Domain;

namespace AnonyMeow.Services.Ranking.Strategies;

// "Pinned" isn't a comment concept, so ApplyToComments just falls back to newest-first — nothing
// in this codebase calls this strategy for comments today.
public class PinnedSortStrategy : ISortStrategy
{
    public IQueryable<Post> ApplyToPosts(IQueryable<Post> posts) =>
        posts.Where(p => p.IsPinned).OrderByDescending(p => p.CreatedAtUtc);

    public IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments) =>
        comments.OrderByDescending(c => c.CreatedAtUtc);
}
