using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class ConversationService(AppDbContext dbContext, IBlockService blockService) : IConversationService
{
    public async Task<Conversation> GetOrCreateAsync(
        Guid userId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
        {
            throw new SelfConversationException();
        }

        if (await blockService.IsBlockedEitherWayAsync(userId, otherUserId, cancellationToken))
        {
            throw new UserBlockedException();
        }

        var (participantAId, participantBId) = userId.CompareTo(otherUserId) <= 0
            ? (userId, otherUserId)
            : (otherUserId, userId);

        var existing = await dbContext.Conversations.SingleOrDefaultAsync(
            c => c.ParticipantAId == participantAId && c.ParticipantBId == participantBId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ParticipantAId = participantAId,
            ParticipantBId = participantBId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<(IReadOnlyList<Conversation> Items, int TotalCount)> ListForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Excludes conversations the user has deleted for themselves (the other participant may
        // still see them fine — deletion is per-user, not global) and sorts pinned-for-this-user
        // conversations to the top.
        var query = dbContext.Conversations.Where(c =>
            (c.ParticipantAId == userId || c.ParticipantBId == userId) &&
            !((c.ParticipantAId == userId && c.DeletedByAAtUtc != null) ||
              (c.ParticipantBId == userId && c.DeletedByBAtUtc != null)));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.ParticipantAId == userId ? c.PinnedByAAtUtc : c.PinnedByBAtUtc)
            .ThenByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Conversations.FindAsync([id], cancellationToken);

    public bool IsParticipant(Conversation conversation, Guid userId) =>
        conversation.ParticipantAId == userId || conversation.ParticipantBId == userId;

    public async Task<Conversation> SetPinnedAsync(
        Guid conversationId, Guid userId, bool pinned, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations.FindAsync([conversationId], cancellationToken)
            ?? throw new ConversationNotFoundException();
        if (!IsParticipant(conversation, userId))
        {
            throw new NotConversationParticipantException();
        }

        var timestamp = pinned ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
        if (conversation.ParticipantAId == userId)
        {
            conversation.PinnedByAAtUtc = timestamp;
        }
        else
        {
            conversation.PinnedByBAtUtc = timestamp;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task DeleteForUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations.FindAsync([conversationId], cancellationToken)
            ?? throw new ConversationNotFoundException();
        if (!IsParticipant(conversation, userId))
        {
            throw new NotConversationParticipantException();
        }

        if (conversation.ParticipantAId == userId)
        {
            conversation.DeletedByAAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            conversation.DeletedByBAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
