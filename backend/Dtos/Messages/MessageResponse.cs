using AnonyMeow.Domain;

namespace AnonyMeow.Dtos.Messages;

public record MessageResponse(
    Guid Id, Guid ConversationId, Guid SenderId, string Body, bool IsRead, DateTimeOffset CreatedAtUtc,
    Guid? ReplyToMessageId, Guid? ReplyToSenderId, string? ReplyToBodyPreview)
{
    private const int ReplyPreviewMaxLength = 140;

    /// <summary>
    /// `replyTo` is the entity `message.ReplyToMessageId` points at, if any — pass null both when
    /// the message isn't a reply and when the replied-to message could no longer be resolved
    /// (removed/deleted), so the client can render "original message unavailable" either way
    /// while still knowing (via ReplyToMessageId) that a reply was intended.
    /// </summary>
    public static MessageResponse FromEntity(Message message, Message? replyTo = null) => new(
        message.Id, message.ConversationId, message.SenderId, message.Body, message.IsRead, message.CreatedAtUtc,
        message.ReplyToMessageId,
        replyTo is { IsRemoved: false } ? replyTo.SenderId : null,
        replyTo is { IsRemoved: false } ? Truncate(replyTo.Body) : null);

    private static string Truncate(string text) =>
        text.Length <= ReplyPreviewMaxLength ? text : text[..ReplyPreviewMaxLength] + "…";
}
