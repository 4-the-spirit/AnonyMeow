using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class BlockService(AppDbContext dbContext) : IBlockService
{
    public async Task BlockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        if (blockerId == blockedId)
        {
            throw new SelfBlockException();
        }

        var existing = await dbContext.UserBlocks.FindAsync([blockerId, blockedId], cancellationToken);
        if (existing is not null)
        {
            return;
        }

        dbContext.UserBlocks.Add(new UserBlock
        {
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UserBlocks.FindAsync([blockerId, blockedId], cancellationToken);
        if (existing is null)
        {
            return;
        }

        dbContext.UserBlocks.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsBlockedEitherWayAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default) =>
        dbContext.UserBlocks.AnyAsync(
            b => (b.BlockerId == userId && b.BlockedId == otherUserId) ||
                 (b.BlockerId == otherUserId && b.BlockedId == userId),
            cancellationToken);
}
