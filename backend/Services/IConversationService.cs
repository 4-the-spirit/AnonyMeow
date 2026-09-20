using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface IConversationService
{
    Task<Conversation> GetOrCreateAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Conversation> Items, int TotalCount)> ListForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    bool IsParticipant(Conversation conversation, Guid userId);

    Task<Conversation> SetPinnedAsync(
        Guid conversationId, Guid userId, bool pinned, CancellationToken cancellationToken = default);

    Task DeleteForUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
