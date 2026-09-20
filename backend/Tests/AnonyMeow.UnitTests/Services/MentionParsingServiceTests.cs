using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class MentionParsingServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AppUser MakeUser(string username) => new()
    {
        Id = Guid.NewGuid(),
        B2CObjectId = Guid.NewGuid().ToString(),
        Username = username,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task ExtractMentionedUsersAsync_ResolvesExistingMentionedUsername()
    {
        var dbContext = CreateDbContext();
        var mentioned = MakeUser("alice");
        dbContext.Users.Add(mentioned);
        await dbContext.SaveChangesAsync();
        var service = new MentionParsingService(dbContext);

        var users = await service.ExtractMentionedUsersAsync("hey @alice, look at this");

        Assert.Equal(mentioned.Id, Assert.Single(users).Id);
    }

    [Fact]
    public async Task ExtractMentionedUsersAsync_IsCaseInsensitive()
    {
        var dbContext = CreateDbContext();
        var mentioned = MakeUser("alice");
        dbContext.Users.Add(mentioned);
        await dbContext.SaveChangesAsync();
        var service = new MentionParsingService(dbContext);

        var users = await service.ExtractMentionedUsersAsync("hey @ALICE, look at this");

        Assert.Equal(mentioned.Id, Assert.Single(users).Id);
    }

    [Fact]
    public async Task ExtractMentionedUsersAsync_IgnoresNonExistentUsernames()
    {
        var dbContext = CreateDbContext();
        var service = new MentionParsingService(dbContext);

        var users = await service.ExtractMentionedUsersAsync("hey @nobody, look at this");

        Assert.Empty(users);
    }

    [Fact]
    public async Task ExtractMentionedUsersAsync_DedupesRepeatedMentionOfSameUser()
    {
        var dbContext = CreateDbContext();
        var mentioned = MakeUser("alice");
        dbContext.Users.Add(mentioned);
        await dbContext.SaveChangesAsync();
        var service = new MentionParsingService(dbContext);

        var users = await service.ExtractMentionedUsersAsync("@alice @alice @alice");

        Assert.Single(users);
    }

    [Fact]
    public async Task ExtractMentionedUsersAsync_ResolvesMultipleDistinctMentions()
    {
        var dbContext = CreateDbContext();
        var alice = MakeUser("alice");
        var bob = MakeUser("bob");
        dbContext.Users.AddRange(alice, bob);
        await dbContext.SaveChangesAsync();
        var service = new MentionParsingService(dbContext);

        var users = await service.ExtractMentionedUsersAsync("@alice and @bob should see this");

        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Id == alice.Id);
        Assert.Contains(users, u => u.Id == bob.Id);
    }

    [Fact]
    public async Task ExtractMentionedUsersAsync_ReturnsEmpty_WhenNoMentions()
    {
        var dbContext = CreateDbContext();
        var service = new MentionParsingService(dbContext);

        var users = await service.ExtractMentionedUsersAsync("no mentions here");

        Assert.Empty(users);
    }
}
