using AnonyMeow.Common.Exceptions;
using AnonyMeow.Services.NativeAuth;

namespace AnonyMeow.UnitTests.Services.NativeAuth;

public class SignInFlowServiceTests
{
    [Fact]
    public async Task StartAsync_ReturnsTokens_OnValidCredentials()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignInWithPassword = (_, _) => new NativeAuthTokenResult("access", "id", "refresh", 3600)
        };
        var service = new SignInFlowService(client);

        var tokens = await service.StartAsync("jane@example.com", "P@ssw0rd123");

        Assert.Equal("access", tokens.AccessToken);
        Assert.Equal("id", tokens.IdToken);
    }

    [Fact]
    public async Task StartAsync_Throws_AccountNotFound_OnUserNotFound()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => throw new NativeAuthApiException("user_not_found", "no such user", null, null)
        };
        var service = new SignInFlowService(client);

        await Assert.ThrowsAsync<NativeAuthAccountNotFoundException>(
            () => service.StartAsync("jane@example.com", "P@ssw0rd123"));
    }

    [Fact]
    public async Task StartAsync_Throws_InvalidCredentials_OnInvalidGrant()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignInWithPassword = (_, _) => throw new NativeAuthApiException("invalid_grant", "wrong password", null, null)
        };
        var service = new SignInFlowService(client);

        await Assert.ThrowsAsync<NativeAuthInvalidCredentialsException>(
            () => service.StartAsync("jane@example.com", "wrong-password"));
    }

    [Fact]
    public async Task StartAsync_Throws_Unavailable_WhenInitiateReturnsRedirect()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => new NativeAuthStepResult("token", "redirect", null, null, null)
        };
        var service = new SignInFlowService(client);

        await Assert.ThrowsAsync<NativeAuthUnavailableException>(
            () => service.StartAsync("jane@example.com", "P@ssw0rd123"));
    }

    [Fact]
    public async Task StartAsync_Throws_Unavailable_WhenChallengeIsNotPassword()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "oob", "email", null, 8)
        };
        var service = new SignInFlowService(client);

        await Assert.ThrowsAsync<NativeAuthUnavailableException>(
            () => service.StartAsync("jane@example.com", "P@ssw0rd123"));
    }
}
