using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Conversations;
using AnonyMeow.Dtos.Messages;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

public static class ConversationEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conversations");

        group.MapPost("", StartConversationAsync).WithName("StartConversation");
        group.MapGet("", ListConversationsAsync).WithName("ListConversations");
        group.MapGet("/{id:guid}", GetConversationAsync).WithName("GetConversation");
        group.MapGet("/{id:guid}/messages", ListMessagesAsync).WithName("ListConversationMessages");
        group.MapPost("/{id:guid}/messages", SendMessageAsync).WithName("SendMessage").RequireRateLimiting("SendMessage");
        group.MapPatch("/{id:guid}/pin", SetConversationPinAsync).WithName("SetConversationPin");
        group.MapDelete("/{id:guid}", DeleteConversationAsync).WithName("DeleteConversation");

        return app;
    }

    private static async Task<AppUser?> FindByUsernameAsync(AppDbContext dbContext, string username, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        return await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
    }

    private static async Task<Results<Created<ConversationResponse>, NotFound>> StartConversationAsync(
        StartConversationRequest request,
        IConversationService conversationService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var other = await FindByUsernameAsync(dbContext, request.Username, cancellationToken);
        if (other is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var conversation = await conversationService.GetOrCreateAsync(currentUser.Id, other.Id, cancellationToken);

        return TypedResults.Created(
            $"/api/conversations/{conversation.Id}",
            ConversationResponse.FromEntity(
                conversation, other.Username ?? string.Empty, other.Id, currentUser.Id, other.LastSeenAt, other.AvatarSeed,
                other.DisplayName));
    }

    private static async Task<Ok<PagedResponse<ConversationResponse>>> ListConversationsAsync(
        IConversationService conversationService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var (items, totalCount) = await conversationService.ListForUserAsync(currentUser.Id, page, DefaultPageSize, cancellationToken);

        var otherUserIds = items
            .Select(c => c.ParticipantAId == currentUser.Id ? c.ParticipantBId : c.ParticipantAId)
            .ToList();
        var otherUsers = await dbContext.Users
            .Where(u => otherUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var responses = items
            .Select(c =>
            {
                var otherId = c.ParticipantAId == currentUser.Id ? c.ParticipantBId : c.ParticipantAId;
                var otherUser = otherUsers.GetValueOrDefault(otherId);
                return ConversationResponse.FromEntity(
                    c, otherUser?.Username ?? string.Empty, otherId, currentUser.Id, otherUser?.LastSeenAt, otherUser?.AvatarSeed,
                    otherUser?.DisplayName);
            })
            .ToList();

        return TypedResults.Ok(new PagedResponse<ConversationResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Results<Ok<ConversationResponse>, NotFound>> GetConversationAsync(
        Guid id,
        IConversationService conversationService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetByIdAsync(id, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (conversation is null || !conversationService.IsParticipant(conversation, currentUser.Id))
        {
            return TypedResults.NotFound();
        }

        var otherId = conversation.ParticipantAId == currentUser.Id ? conversation.ParticipantBId : conversation.ParticipantAId;
        var otherUser = await dbContext.Users.FindAsync([otherId], cancellationToken);

        return TypedResults.Ok(ConversationResponse.FromEntity(
            conversation, otherUser?.Username ?? string.Empty, otherId, currentUser.Id, otherUser?.LastSeenAt, otherUser?.AvatarSeed,
            otherUser?.DisplayName));
    }

    private static async Task<Results<Ok<ConversationResponse>, NotFound>> SetConversationPinAsync(
        Guid id,
        SetConversationPinRequest request,
        IConversationService conversationService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetByIdAsync(id, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (conversation is null || !conversationService.IsParticipant(conversation, currentUser.Id))
        {
            return TypedResults.NotFound();
        }

        conversation = await conversationService.SetPinnedAsync(id, currentUser.Id, request.Pinned, cancellationToken);
        var otherId = conversation.ParticipantAId == currentUser.Id ? conversation.ParticipantBId : conversation.ParticipantAId;
        var otherUser = await dbContext.Users.FindAsync([otherId], cancellationToken);

        return TypedResults.Ok(ConversationResponse.FromEntity(
            conversation, otherUser?.Username ?? string.Empty, otherId, currentUser.Id, otherUser?.LastSeenAt, otherUser?.AvatarSeed,
            otherUser?.DisplayName));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteConversationAsync(
        Guid id,
        IConversationService conversationService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetByIdAsync(id, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (conversation is null || !conversationService.IsParticipant(conversation, currentUser.Id))
        {
            return TypedResults.NotFound();
        }

        await conversationService.DeleteForUserAsync(id, currentUser.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<PagedResponse<MessageResponse>>, NotFound>> ListMessagesAsync(
        Guid id,
        IConversationService conversationService,
        IMessageService messageService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        int page = 1)
    {
        var conversation = await conversationService.GetByIdAsync(id, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (conversation is null || !conversationService.IsParticipant(conversation, currentUser.Id))
        {
            return TypedResults.NotFound();
        }

        page = Math.Max(page, 1);
        var (items, totalCount) = await messageService.ListAsync(id, page, DefaultPageSize, cancellationToken);

        var replyIds = items.Where(m => m.ReplyToMessageId is not null).Select(m => m.ReplyToMessageId!.Value).Distinct().ToList();
        var replyTargets = await dbContext.Messages
            .Where(m => replyIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var responses = items
            .Select(m => MessageResponse.FromEntity(
                m, m.ReplyToMessageId is { } replyId ? replyTargets.GetValueOrDefault(replyId) : null))
            .ToList();

        return TypedResults.Ok(new PagedResponse<MessageResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Results<Created<MessageResponse>, NotFound>> SendMessageAsync(
        Guid id,
        SendMessageRequest request,
        IConversationService conversationService,
        IMessageService messageService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetByIdAsync(id, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (conversation is null || !conversationService.IsParticipant(conversation, currentUser.Id))
        {
            return TypedResults.NotFound();
        }

        var (message, replyTo) = await messageService.SendAsync(
            id, currentUser.Id, request.Body, request.ReplyToMessageId, cancellationToken);
        return TypedResults.Created(
            $"/api/conversations/{id}/messages/{message.Id}", MessageResponse.FromEntity(message, replyTo));
    }
}
