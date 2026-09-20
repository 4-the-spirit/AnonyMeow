using AnonyMeow.Domain;

namespace AnonyMeow.Dtos.Conversations;

public record ConversationResponse(
    Guid Id,
    string OtherUsername,
    string? OtherUserDisplayName,
    Guid OtherUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? OtherUserLastSeenAt,
    string? OtherUserAvatarSeed,
    bool IsPinned)
{
    public static ConversationResponse FromEntity(
        Conversation conversation, string otherUsername, Guid otherUserId, Guid currentUserId,
        DateTimeOffset? otherUserLastSeenAt = null, string? otherUserAvatarSeed = null,
        string? otherUserDisplayName = null) =>
        new(conversation.Id, otherUsername, otherUserDisplayName, otherUserId, conversation.CreatedAtUtc, otherUserLastSeenAt,
            otherUserAvatarSeed, conversation.IsPinnedBy(currentUserId));
}
