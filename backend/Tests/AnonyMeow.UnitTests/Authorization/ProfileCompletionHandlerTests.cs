using System.Security.Claims;
using AnonyMeow.Common.Authorization;
using AnonyMeow.Domain;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;

namespace AnonyMeow.UnitTests.Authorization;

public class ProfileCompletionHandlerTests
{
    private class FakeCurrentUserAccessor(AppUser? user) : ICurrentUserAccessor
    {
        public bool WasCalled { get; private set; }

        public Task<AppUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(user!);
        }

        public Task<AppUser?> GetCurrentUserOrNullAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(user);
        }
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal principal) =>
        new([new ProfileCompletionRequirement()], principal, resource: null);

    [Fact]
    public async Task Succeeds_WhenAuthenticatedUserHasUsername()
    {
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = "oid-1", Username = "someuser", CreatedAtUtc = DateTimeOffset.UtcNow };
        var handler = new ProfileCompletionHandler(new FakeCurrentUserAccessor(user));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "oid-1")], "TestAuth"));
        var context = CreateContext(principal);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DoesNotSucceed_WhenAuthenticatedUserHasNoUsername()
    {
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = "oid-1", Username = null, CreatedAtUtc = DateTimeOffset.UtcNow };
        var handler = new ProfileCompletionHandler(new FakeCurrentUserAccessor(user));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "oid-1")], "TestAuth"));
        var context = CreateContext(principal);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task DoesNotSucceed_AndSkipsLookup_WhenNotAuthenticated()
    {
        var accessor = new FakeCurrentUserAccessor(null);
        var handler = new ProfileCompletionHandler(accessor);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(principal);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.False(accessor.WasCalled);
    }
}
