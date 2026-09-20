using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface IMessageService
{
    Task<(Message Message, Message? ReplyTo)> SendAsync(
        Guid conversationId, Guid senderId, string body, Guid? replyToMessageId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Message> Items, int TotalCount)> ListAsync(
        Guid conversationId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
