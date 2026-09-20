using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class UsernameReservationServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Theory]
    [InlineData("abc")]
    [InlineData("ab_123")]
    [InlineData("a2345678901234567890")]
    public void IsValidFormat_AcceptsAllowedShapes(string username)
    {
        var service = new UsernameReservationService(CreateDbContext());

        Assert.True(service.IsValidFormat(username));
    }

    [Theory]
    [InlineData("ab")] // too short
    [InlineData("a234567890123456789012")] // too long (22 chars)
    [InlineData("Abc123")] // uppercase not allowed
    [InlineData("abc-123")] // disallowed character
    [InlineData("")]
    public void IsValidFormat_RejectsDisallowedShapes(string username)
    {
        var service = new UsernameReservationService(CreateDbContext());

        Assert.False(service.IsValidFormat(username));
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue_WhenNoCollision()
    {
        var service = new UsernameReservationService(CreateDbContext());

        Assert.True(await service.IsAvailableAsync("freeusername"));
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalse_OnCaseInsensitiveCollision()
    {
        var dbContext = CreateDbContext();
        dbContext.Users.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            B2CObjectId = Guid.NewGuid().ToString(),
            Username = "TakenName",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new UsernameReservationService(dbContext);

        Assert.False(await service.IsAvailableAsync("takenname"));
    }

    [Fact]
    public async Task ReserveAsync_SetsUsername_WhenAvailable()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            B2CObjectId = Guid.NewGuid().ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new UsernameReservationService(dbContext);
        await service.ReserveAsync(user, "newusername");

        Assert.Equal("newusername", user.Username);
    }

    [Fact]
    public async Task ReserveAsync_Throws_OnCollision()
    {
        var dbContext = CreateDbContext();
        dbContext.Users.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            B2CObjectId = Guid.NewGuid().ToString(),
            Username = "takenname",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            B2CObjectId = Guid.NewGuid().ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new UsernameReservationService(dbContext);

        await Assert.ThrowsAsync<UsernameConflictException>(() => service.ReserveAsync(user, "takenname"));
    }
}
