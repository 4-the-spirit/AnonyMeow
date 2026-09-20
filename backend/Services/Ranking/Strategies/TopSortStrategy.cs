using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.Ranking.Strategies;

public class TopSortStrategy(AppDbContext dbContext) : ISortStrategy
{
    public IQueryable<Post> ApplyToPosts(IQueryable<Post> posts) =>
        posts.OrderByDescending(p =>
            dbContext.Votes
                .Where(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id)
                .Sum(v => (int)v.Value));

    public IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments) =>
        comments.OrderByDescending(c =>
            dbContext.Votes
                .Where(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id)
                .Sum(v => (int)v.Value));
}
