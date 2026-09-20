using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.Ranking.Strategies;

public class ControversialSortStrategy(AppDbContext dbContext) : ISortStrategy
{
    // Placeholder tunable: min(up,down)/max(up,down,1) — favors a near-even up/down split. The
    // parent plan's fuller formula also weights by total vote volume via log10(up+down+1);
    // dropped here since Math.Log10 isn't reliably SQL-translatable and the formula is already
    // flagged as tunable/placeholder pending real engagement data.
    public IQueryable<Post> ApplyToPosts(IQueryable<Post> posts) =>
        posts.OrderByDescending(p =>
            (double)Math.Min(
                dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id && v.Value > 0),
                dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id && v.Value < 0))
            / Math.Max(
                Math.Max(
                    dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id && v.Value > 0),
                    dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Post && v.TargetId == p.Id && v.Value < 0)),
                1));

    public IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments) =>
        comments.OrderByDescending(c =>
            (double)Math.Min(
                dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id && v.Value > 0),
                dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id && v.Value < 0))
            / Math.Max(
                Math.Max(
                    dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id && v.Value > 0),
                    dbContext.Votes.Count(v => v.TargetType == VoteTargetType.Comment && v.TargetId == c.Id && v.Value < 0)),
                1));
}
