using AnonyMeow.Domain;

namespace AnonyMeow.Services.Ranking.Strategies;

public class NewSortStrategy : ISortStrategy
{
    public IQueryable<Post> ApplyToPosts(IQueryable<Post> posts) => posts.OrderByDescending(p => p.CreatedAtUtc);

    public IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments) =>
        comments.OrderByDescending(c => c.CreatedAtUtc);
}
