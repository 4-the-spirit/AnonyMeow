using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Messages;
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Hubs;
using AnonyMeow.Services;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class MessageServiceTests
{
    // Hand-rolled IHubContext fake — no mocking library in this project, same convention as
    // NotificationDispatcherTests.
    private class FakeNotificationClient : INotificationClient
    {
        public List<MessageResponse> ReceivedMessages { get; } = [];

        public Task ReceiveNotification(NotificationResponse notification) => Task.CompletedTask;

        public Task ReceiveMessage(MessageResponse message)
        {
            ReceivedMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private class FakeHubClients : IHubClients<INotificationClient>
    {
        private readonly Dictionary<string, FakeNotificationClient> _groups = [];

        public IReadOnlyDictionary<string, FakeNotificationClient> JoinedGroups => _groups;

        public INotificationClient All => throw new NotSupportedException();
        public INotificationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public INotificationClient Client(string connectionId) => throw new NotSupportedException();
        public INotificationClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public INotificationClient Group(string groupName)
        {
            if (!_groups.TryGetValue(groupName, out var client))
            {
                client = new FakeNotificationClient();
                _groups[groupName] = client;
            }

            return client;
        }

        public INotificationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public INotificationClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public INotificationClient OthersInGroup(string groupName) => throw new NotSupportedException();
        public INotificationClient User(string userId) => throw new NotSupportedException();
        public INotificationClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private class FakeHubContext : IHubContext<NotificationHub, INotificationClient>
    {
        public FakeHubClients FakeClients { get; } = new();
        public IHubClients<INotificationClient> Clients => FakeClients;
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private class FakeContentSubmissionPipeline(bool block) : IContentSubmissionPipeline
    {
        public Task<ContentSubmissionResult> EvaluateAsync(ContentSubmissionRequest submission, CancellationToken cancellationToken = default) =>
            Task.FromResult(block ? new ContentSubmissionResult(true, ["Email"]) : ContentSubmissionResult.Allowed);
    }

    private class FakeSpamFlaggingService : ISpamFlaggingService
    {
        public Task FlagIfSpamAsync(
            SpamFlagTargetType targetType, Guid targetId, Guid authorId, ContentSubmissionType contentType, string text,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser A, AppUser B, Conversation Conversation)> SeedConversationAsync(AppDbContext dbContext)
    {
        var a = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "usera", CreatedAtUtc = DateTimeOffset.UtcNow };
        var b = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "userb", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(a, b);
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ParticipantAId = a.Id,
            ParticipantBId = b.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();
        return (a, b, conversation);
    }

    [Fact]
    public async Task SendAsync_Throws_WhenConversationDoesNotExist()
    {
        var dbContext = CreateDbContext();
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());

        await Assert.ThrowsAsync<ConversationNotFoundException>(
            () => service.SendAsync(Guid.NewGuid(), Guid.NewGuid(), "hi"));
    }

    [Fact]
    public async Task SendAsync_Throws_WhenSenderIsNotAParticipant()
    {
        var dbContext = CreateDbContext();
        var (_, _, conversation) = await SeedConversationAsync(dbContext);
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());

        await Assert.ThrowsAsync<NotConversationParticipantException>(
            () => service.SendAsync(conversation.Id, Guid.NewGuid(), "hi"));
    }

    [Fact]
    public async Task SendAsync_PersistsMessage_AndPushesToOtherParticipantsGroup()
    {
        var dbContext = CreateDbContext();
        var (a, b, conversation) = await SeedConversationAsync(dbContext);
        var hubContext = new FakeHubContext();
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), hubContext);

        var (message, replyTo) = await service.SendAsync(conversation.Id, a.Id, "hello there");

        Assert.Null(replyTo);
        Assert.Equal("hello there", (await dbContext.Messages.SingleAsync()).Body);
        var group = Assert.Single(hubContext.FakeClients.JoinedGroups);
        Assert.Equal(NotificationHub.GroupName(b.Id), group.Key);
        Assert.Equal(message.Id, Assert.Single(group.Value.ReceivedMessages).Id);
    }

    [Fact]
    public async Task SendAsync_Throws_WhenBlockedAfterConversationAlreadyExists()
    {
        var dbContext = CreateDbContext();
        var (a, b, conversation) = await SeedConversationAsync(dbContext);
        var blockService = new BlockService(dbContext);
        await blockService.BlockAsync(b.Id, a.Id);
        var service = new MessageService(dbContext, blockService, new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());

        await Assert.ThrowsAsync<UserBlockedException>(() => service.SendAsync(conversation.Id, a.Id, "hi"));
    }

    [Fact]
    public async Task SendAsync_Throws_WhenContentSubmissionPipelineBlocks()
    {
        var dbContext = CreateDbContext();
        var (a, _, conversation) = await SeedConversationAsync(dbContext);
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(true), new FakeSpamFlaggingService(), new FakeHubContext());

        var ex = await Assert.ThrowsAsync<PiiDetectedException>(() => service.SendAsync(conversation.Id, a.Id, "email me at a@b.com"));
        Assert.Contains("Email", ex.Extensions["detectedCategories"] as IReadOnlyList<string> ?? []);
    }

    [Fact]
    public async Task SendAsync_WithValidReplyTarget_PersistsReplyAndReturnsIt()
    {
        var dbContext = CreateDbContext();
        var (a, b, conversation) = await SeedConversationAsync(dbContext);
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());
        var (original, _) = await service.SendAsync(conversation.Id, a.Id, "original message");

        var (reply, replyTo) = await service.SendAsync(conversation.Id, b.Id, "replying to you", original.Id);

        Assert.Equal(original.Id, reply.ReplyToMessageId);
        Assert.NotNull(replyTo);
        Assert.Equal(original.Id, replyTo!.Id);
    }

    [Fact]
    public async Task SendAsync_Throws_WhenReplyTargetIsInAnotherConversation()
    {
        var dbContext = CreateDbContext();
        var (a, b, conversation) = await SeedConversationAsync(dbContext);
        var (_, otherB, otherConversation) = await SeedConversationAsync(dbContext);
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());
        var (foreignMessage, _) = await service.SendAsync(otherConversation.Id, otherB.Id, "in a different conversation");

        await Assert.ThrowsAsync<InvalidReplyTargetException>(
            () => service.SendAsync(conversation.Id, a.Id, "hi", foreignMessage.Id));
    }

    [Fact]
    public async Task SendAsync_Throws_WhenReplyTargetDoesNotExist()
    {
        var dbContext = CreateDbContext();
        var (a, _, conversation) = await SeedConversationAsync(dbContext);
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());

        await Assert.ThrowsAsync<InvalidReplyTargetException>(
            () => service.SendAsync(conversation.Id, a.Id, "hi", Guid.NewGuid()));
    }

    [Fact]
    public async Task SendAsync_RevivesConversation_HiddenByEitherParticipant()
    {
        var dbContext = CreateDbContext();
        var (a, b, conversation) = await SeedConversationAsync(dbContext);
        var conversationService = new ConversationService(dbContext, new BlockService(dbContext));
        await conversationService.DeleteForUserAsync(conversation.Id, a.Id);
        var service = new MessageService(dbContext, new BlockService(dbContext), new FakeContentSubmissionPipeline(false), new FakeSpamFlaggingService(), new FakeHubContext());

        await service.SendAsync(conversation.Id, b.Id, "still here?");

        var reloaded = await dbContext.Conversations.FindAsync(conversation.Id);
        Assert.False(reloaded!.IsDeletedBy(a.Id));
        Assert.False(reloaded.IsDeletedBy(b.Id));
    }
}
