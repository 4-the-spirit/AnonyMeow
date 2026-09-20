using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Messages;
using AnonyMeow.Hubs;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class MessageService(
    AppDbContext dbContext,
    IBlockService blockService,
    IContentSubmissionPipeline contentSubmissionPipeline,
    ISpamFlaggingService spamFlaggingService,
    IHubContext<NotificationHub, INotificationClient> hubContext) : IMessageService
{
    public async Task<(Message Message, Message? ReplyTo)> SendAsync(
        Guid conversationId, Guid senderId, string body, Guid? replyToMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations.FindAsync([conversationId], cancellationToken)
            ?? throw new ConversationNotFoundException();

        var otherParticipantId = conversation.ParticipantAId == senderId
            ? conversation.ParticipantBId
            : conversation.ParticipantAId;
        if (conversation.ParticipantAId != senderId && conversation.ParticipantBId != senderId)
        {
            throw new NotConversationParticipantException();
        }

        // Re-checked at send-time, not just conversation-create time — a block can happen after a
        // conversation already exists, per the plan's explicit "enforced at conversation-create
        // and message-send" requirement.
        if (await blockService.IsBlockedEitherWayAsync(senderId, otherParticipantId, cancellationToken))
        {
            throw new UserBlockedException();
        }

        Message? replyTo = null;
        if (replyToMessageId is { } replyId)
        {
            replyTo = await dbContext.Messages.FindAsync([replyId], cancellationToken);
            // Must belong to this same conversation — otherwise a caller could forge a reply
            // pointer into a conversation they aren't even part of.
            if (replyTo is null || replyTo.ConversationId != conversationId)
            {
                throw new InvalidReplyTargetException();
            }
        }

        var evaluation = await contentSubmissionPipeline.EvaluateAsync(
            new ContentSubmissionRequest(ContentSubmissionType.DirectMessage, senderId, body), cancellationToken);
        if (evaluation.IsBlocked)
        {
            throw new PiiDetectedException(evaluation.DetectedCategories);
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Body = body,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReplyToMessageId = replyToMessageId
        };
        dbContext.Messages.Add(message);

        // New activity revives the conversation for both sides — otherwise a conversation one
        // participant deleted would stay hidden from their list even after the other person
        // replies, which isn't the "delete just clears it from view" behavior users expect.
        conversation.DeletedByAAtUtc = null;
        conversation.DeletedByBAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Post-save, same reasoning as PostService/CommentService: a flagged message still gets
        // delivered, just flagged for review.
        await spamFlaggingService.FlagIfSpamAsync(
            SpamFlagTargetType.DirectMessage, message.Id, senderId, ContentSubmissionType.DirectMessage, body, cancellationToken);

        await hubContext.Clients
            .Group(NotificationHub.GroupName(otherParticipantId))
            .ReceiveMessage(MessageResponse.FromEntity(message, replyTo));

        return (message, replyTo);
    }

    public async Task<(IReadOnlyList<Message> Items, int TotalCount)> ListAsync(
        Guid conversationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Messages.Where(m => m.ConversationId == conversationId && !m.IsRemoved);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Messages.FindAsync([id], cancellationToken);
}
