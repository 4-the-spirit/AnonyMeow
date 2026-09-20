using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class SavedCommentService(AppDbContext dbContext) : ISavedCommentService
{
    public async Task SaveAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.SavedComments.AnyAsync(
            s => s.AppUserId == userId && s.CommentId == commentId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.SavedComments.Add(new SavedComment
        {
            AppUserId = userId,
            CommentId = commentId,
            SavedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnsaveAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedComments.FindAsync([userId, commentId], cancellationToken);
        if (saved is null)
        {
            return;
        }

        dbContext.SavedComments.Remove(saved);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListSavedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.SavedComments.CountAsync(s => s.AppUserId == userId, cancellationToken);

        var items = await dbContext.SavedComments
            .Where(s => s.AppUserId == userId)
            .Join(dbContext.Comments, s => s.CommentId, c => c.Id, (s, c) => new { s.SavedAtUtc, Comment = c })
            .OrderByDescending(x => x.SavedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Comment)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
