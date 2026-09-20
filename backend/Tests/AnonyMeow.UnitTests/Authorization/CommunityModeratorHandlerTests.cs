using System.Security.Claims;
using AnonyMeow.Common.Authorization;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Authorization;

public class CommunityModeratorHandlerTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private class FakeCurrentUserAccessor(AppUser user) : ICurrentUserAccessor
    {
        public Task<AppUser> GetCurrentUserAsync(CancellationToken cancellationToken = default) => Task.FromResult(user);
        public Task<AppUser?> GetCurrentUserOrNullAsync(CancellationToken cancellationToken = default) => Task.FromResult<AppUser?>(user);
    }

    private static ClaimsPrincipal AuthenticatedPrincipal() =>
        new(new ClaimsIdentity([new Claim("oid", "oid-1")], "TestAuth"));

    [Fact]
    public async Task Succeeds_WhenUserIsModeratorOfResource()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = "oid-1", Username = "mod", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        dbContext.Users.Add(user);
        dbContext.Communities.Add(community);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id, AppUserId = user.Id, Role = CommunityRole.Moderator, JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = new CommunityModeratorHandler(new FakeCurrentUserAccessor(user), dbContext);
        var context = new AuthorizationHandlerContext([new CommunityModeratorRequirement()], AuthenticatedPrincipal(), community);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DoesNotSucceed_WhenUserIsOnlyAMember()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = "oid-1", Username = "member", CreatedAtUtc = DateTimeOffset.UtcNow };
        var otherCreator = Guid.NewGuid();
        var community = new Community { Id = Guid.NewGuid(), Name = "c2", CreatedByUserId = otherCreator, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        dbContext.Users.Add(user);
        dbContext.Communities.Add(community);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id, AppUserId = user.Id, Role = CommunityRole.Member, JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = new CommunityModeratorHandler(new FakeCurrentUserAccessor(user), dbContext);
        var context = new AuthorizationHandlerContext([new CommunityModeratorRequirement()], AuthenticatedPrincipal(), community);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
