using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.Ranking.Strategies;

public class HotSortStrategy(AppDbContext dbContext) : ISortStrategy
{
    // Placeholder tunable (parent plan explicitly defers picking a real decay constant until
    // there's usage data): linear decay against a 12-hour half-life, rather than the classic
    // Reddit log-scaled formula — avoids relying on Math.Log10/Math.Sign SQL translation for a
    // formula that's going to be retuned anyway.
    private const double DecayHours = 12.0;

    public IQueryable<Post> ApplyToPosts(IQueryable<Post> posts) =>
        posts.OrderByDescending(p =>
            (double)dbContext.Votes
                .Where(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id)
                .Sum(v => (int)v.Value)
            - (DateTimeOffset.UtcNow - p.CreatedAtUtc).TotalHours / DecayHours);

    public IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments) =>
        comments.OrderByDescending(c =>
            (double)dbContext.Votes
                .Where(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id)
                .Sum(v => (int)v.Value)
            - (DateTimeOffset.UtcNow - c.CreatedAtUtc).TotalHours / DecayHours);
}
