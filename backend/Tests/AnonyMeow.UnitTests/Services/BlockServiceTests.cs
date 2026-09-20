using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class BlockServiceTests
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
    public async Task BlockAsync_Throws_OnSelfBlock()
    {
        var dbContext = CreateDbContext();
        var (a, _) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);

        await Assert.ThrowsAsync<SelfBlockException>(() => service.BlockAsync(a.Id, a.Id));
    }

    [Fact]
    public async Task BlockAsync_CreatesBlockRow()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);

        await service.BlockAsync(a.Id, b.Id);

        Assert.Equal(1, await dbContext.UserBlocks.CountAsync());
    }

    [Fact]
    public async Task BlockAsync_CalledTwice_IsIdempotent()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);

        await service.BlockAsync(a.Id, b.Id);
        await service.BlockAsync(a.Id, b.Id);

        Assert.Equal(1, await dbContext.UserBlocks.CountAsync());
    }

    [Fact]
    public async Task UnblockAsync_RemovesBlockRow()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);
        await service.BlockAsync(a.Id, b.Id);

        await service.UnblockAsync(a.Id, b.Id);

        Assert.Equal(0, await dbContext.UserBlocks.CountAsync());
    }

    [Fact]
    public async Task UnblockAsync_NoExistingBlock_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);

        await service.UnblockAsync(a.Id, b.Id);

        Assert.Equal(0, await dbContext.UserBlocks.CountAsync());
    }

    [Fact]
    public async Task IsBlockedEitherWayAsync_ReturnsTrue_RegardlessOfDirection()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);
        await service.BlockAsync(a.Id, b.Id);

        Assert.True(await service.IsBlockedEitherWayAsync(a.Id, b.Id));
        Assert.True(await service.IsBlockedEitherWayAsync(b.Id, a.Id));
    }

    [Fact]
    public async Task IsBlockedEitherWayAsync_ReturnsFalse_WhenNoBlockExists()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = new BlockService(dbContext);

        Assert.False(await service.IsBlockedEitherWayAsync(a.Id, b.Id));
    }
}
