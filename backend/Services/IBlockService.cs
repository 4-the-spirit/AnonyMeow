namespace AnonyMeow.Services;

public interface IBlockService
{
    Task BlockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);

    Task UnblockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);

    Task<bool> IsBlockedEitherWayAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);
}
