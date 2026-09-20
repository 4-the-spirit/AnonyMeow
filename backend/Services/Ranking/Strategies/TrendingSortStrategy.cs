using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.Ranking.Strategies;

public class TrendingSortStrategy(AppDbContext dbContext) : ISortStrategy
{
    // Trending surfaces engagement, not just vote total — comments count as a lighter-weight
    // signal (half a vote) so a heavily-discussed item can outrank a highly-voted but quiet one,
    // distinguishing this from Top's pure vote count. DiscoveryService applies the day/week
    // window as a filter before this ordering runs.
    private const double CommentWeight = 0.5;

    public IQueryable<Post> ApplyToPosts(IQueryable<Post> posts) =>
        posts.OrderByDescending(p =>
            dbContext.Votes.Where(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id).Sum(v => (int)v.Value)
            + dbContext.Comments.Count(c => c.PostId == p.Id && !c.IsRemoved) * CommentWeight);

    public IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments) =>
        comments.OrderByDescending(c =>
            dbContext.Votes.Where(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id).Sum(v => (int)v.Value));
}
