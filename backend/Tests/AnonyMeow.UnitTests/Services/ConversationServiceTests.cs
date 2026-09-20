using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class ConversationServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser A, AppUser B)> SeedUsersAsync(AppDbContext dbContext)
    {
        var a = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "usera", CreatedAtUtc = DateTimeOffset.UtcNow };
        var b = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "userb", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(a, b);
        await dbContext.SaveChangesAsync();
        return (a, b);
    }

    [Fact]
    public async Task GetOrCreateAsync_Throws_OnSelfConversation()
    {
        var dbContext = CreateDbContext();
        var (a, _) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));

        await Assert.ThrowsAsync<SelfConversationException>(() => service.GetOrCreateAsync(a.Id, a.Id));
    }

    [Fact]
    public async Task GetOrCreateAsync_Throws_WhenEitherUserHasBlockedTheOther()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var blockService = new BlockService(dbContext);
        await blockService.BlockAsync(b.Id, a.Id);
        var service = new ConversationService(dbContext, blockService);

        await Assert.ThrowsAsync<UserBlockedException>(() => service.GetOrCreateAsync(a.Id, b.Id));
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesConversation_WithNormalizedParticipantOrder()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));

        var conversation = await service.GetOrCreateAsync(a.Id, b.Id);

        var expectedFirst = a.Id.CompareTo(b.Id) <= 0 ? a.Id : b.Id;
        Assert.Equal(expectedFirst, conversation.ParticipantAId);
    }

    [Fact]
    public async Task GetOrCreateAsync_IsIdempotent_RegardlessOfCallerOrder()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));

        var first = await service.GetOrCreateAsync(a.Id, b.Id);
        var second = await service.GetOrCreateAsync(b.Id, a.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await dbContext.Conversations.CountAsync());
    }

    [Fact]
    public void IsParticipant_ReturnsTrueForBothParticipants_FalseForOthers()
    {
        var dbContext = CreateDbContext();
        var service = new ConversationService(dbContext, new BlockService(dbContext));
        var conversation = new Conversation { Id = Guid.NewGuid(), ParticipantAId = Guid.NewGuid(), ParticipantBId = Guid.NewGuid() };

        Assert.True(service.IsParticipant(conversation, conversation.ParticipantAId));
        Assert.True(service.IsParticipant(conversation, conversation.ParticipantBId));
        Assert.False(service.IsParticipant(conversation, Guid.NewGuid()));
    }

    [Fact]
    public async Task SetPinnedAsync_PinsOnlyForTheCallingParticipant()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));
        var conversation = await service.GetOrCreateAsync(a.Id, b.Id);

        await service.SetPinnedAsync(conversation.Id, a.Id, true);

        var reloaded = await service.GetByIdAsync(conversation.Id);
        Assert.True(reloaded!.IsPinnedBy(a.Id));
        Assert.False(reloaded.IsPinnedBy(b.Id));
    }

    [Fact]
    public async Task SetPinnedAsync_Unpinning_ClearsThePinTimestamp()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));
        var conversation = await service.GetOrCreateAsync(a.Id, b.Id);
        await service.SetPinnedAsync(conversation.Id, a.Id, true);

        await service.SetPinnedAsync(conversation.Id, a.Id, false);

        var reloaded = await service.GetByIdAsync(conversation.Id);
        Assert.False(reloaded!.IsPinnedBy(a.Id));
    }

    [Fact]
    public async Task SetPinnedAsync_Throws_WhenCallerIsNotAParticipant()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));
        var conversation = await service.GetOrCreateAsync(a.Id, b.Id);

        await Assert.ThrowsAsync<NotConversationParticipantException>(
            () => service.SetPinnedAsync(conversation.Id, Guid.NewGuid(), true));
    }

    [Fact]
    public async Task DeleteForUserAsync_HidesConversation_OnlyForThatUser()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new ConversationService(dbContext, new BlockService(dbContext));
        var conversation = await service.GetOrCreateAsync(a.Id, b.Id);

        await service.DeleteForUserAsync(conversation.Id, a.Id);

        var (aItems, _) = await service.ListForUserAsync(a.Id, 1, 20);
        var (bItems, _) = await service.ListForUserAsync(b.Id, 1, 20);
        Assert.DoesNotContain(aItems, c => c.Id == conversation.Id);
        Assert.Contains(bItems, c => c.Id == conversation.Id);
    }

    [Fact]
    public async Task ListForUserAsync_SortsPinnedConversationsFirst()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var c = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "userc", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(c);
        await dbContext.SaveChangesAsync();
        var service = new ConversationService(dbContext, new BlockService(dbContext));
        var withB = await service.GetOrCreateAsync(a.Id, b.Id);
        var withC = await service.GetOrCreateAsync(a.Id, c.Id);

        // withC is created second (so would sort first by recency alone) — pinning withB proves
        // pin status, not just creation order, drives the sort.
        await service.SetPinnedAsync(withB.Id, a.Id, true);

        var (items, _) = await service.ListForUserAsync(a.Id, 1, 20);

        Assert.Equal(withB.Id, items[0].Id);
        Assert.Equal(withC.Id, items[1].Id);
    }
}
