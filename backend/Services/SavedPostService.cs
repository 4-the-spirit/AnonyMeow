using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class SavedPostService(AppDbContext dbContext) : ISavedPostService
{
    public async Task SaveAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.SavedPosts.AnyAsync(
            s => s.AppUserId == userId && s.PostId == postId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.SavedPosts.Add(new SavedPost
        {
            AppUserId = userId,
            PostId = postId,
            SavedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnsaveAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedPosts.FindAsync([userId, postId], cancellationToken);
        if (saved is null)
        {
            return;
        }

        dbContext.SavedPosts.Remove(saved);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> ListSavedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.SavedPosts.CountAsync(s => s.AppUserId == userId, cancellationToken);

        var items = await dbContext.SavedPosts
            .Where(s => s.AppUserId == userId)
            .Join(dbContext.Posts, s => s.PostId, p => p.Id, (s, p) => new { s.SavedAtUtc, Post = p })
            .OrderByDescending(x => x.SavedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Post)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
