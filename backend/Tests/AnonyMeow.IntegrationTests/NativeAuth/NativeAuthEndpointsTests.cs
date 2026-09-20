using System.Net;
using System.Net.Http.Json;
using AnonyMeow.Common.Development;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Dtos.NativeAuth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Services.NativeAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnonyMeow.IntegrationTests.NativeAuth;

// Exercises real routing/DI/rate-limiting/ProblemDetails wiring for the native-auth proxy
// endpoints, with INativeAuthClient swapped for a fake (no live Microsoft tenant involved) —
// mirrors ProfileEndpointsTests' anonymous-call style since every route here is AllowAnonymous.
[Collection(IntegrationTestCollection.Name)]
public class NativeAuthEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private WebApplicationFactory<Program> CreateFactory(FakeNativeAuthClient fakeClient)
    {
        var baseFactory = new CustomWebApplicationFactory(postgresFixture.ConnectionString);
        return baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INativeAuthClient>();
                services.AddSingleton<INativeAuthClient>(fakeClient);
            }));
    }

    [Fact]
    public async Task SignUpStart_ReturnsVerifyEmailStep_WhenOtpChallengeRequired()
    {
        var fake = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => new NativeAuthStepResult("start-token", "none", null, null, null),
            OnSignUpChallenge = _ => new NativeAuthStepResult("challenge-token", "oob", "email", "j***@example.com", 8)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/native/signup/start",
            new SignUpStartRequest("jane@example.com", "P@ssw0rd123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SignUpStartResponse>();
        Assert.Equal("verifyEmail", body!.NextStep);
        Assert.Equal("challenge-token", body.OtpChallenge!.ContinuationToken);
        Assert.Equal(8, body.OtpChallenge.CodeLength);
    }

    [Fact]
    public async Task SignUpStart_Returns422_WhenEmailMissing()
    {
        using var factory = CreateFactory(new FakeNativeAuthClient());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/native/signup/start",
            new SignUpStartRequest("", "P@ssw0rd123"));

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("email"));
    }

    [Fact]
    public async Task SignUpStart_Returns409_WhenEmailAlreadyExists()
    {
        var fake = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => throw new NativeAuthApiException("user_already_exists", "exists", null, null)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/native/signup/start",
            new SignUpStartRequest("jane@example.com", "P@ssw0rd123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email Already Registered", problem!.Title);
    }

    [Fact]
    public async Task SignUpVerifyEmail_ExceedsNamedRateLimitPolicy_ReturnsTooManyRequests()
    {
        var fake = new FakeNativeAuthClient
        {
            OnSignUpContinueWithOob = (_, _) => throw new NativeAuthApiException("invalid_grant", "bad code", "invalid_oob_value", null)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        // appsettings.json's "SignUpVerify" named policy defaults to PermitLimit=5 within a
        // 600-second fixed window — the 6th verify attempt in the same window is rejected.
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/native/signup/verify-email",
                new SignUpVerifyEmailRequest("some-continuation-token", "000000"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    [Fact]
    public async Task SignUpVerifyEmail_UsesIndependentRateLimitBucket_FromSignUpStart()
    {
        var fake = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => throw new NativeAuthApiException("user_already_exists", "exists", null, null),
            OnSignUpContinueWithOob = (_, _) => throw new NativeAuthApiException("invalid_grant", "bad code", "invalid_oob_value", null)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        // Exhaust the separate "SignUpStart" bucket (PermitLimit=5/600s)...
        HttpResponseMessage? lastStartResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastStartResponse = await client.PostAsJsonAsync("/api/auth/native/signup/start",
                new SignUpStartRequest($"jane{i}@example.com", "P@ssw0rd123"));
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, lastStartResponse!.StatusCode);

        // ...a fresh signup's verify-email call must still go through: it was still on
        // "SignUpVerify", a bucket the /start calls above never touched. Before this endpoint had
        // its own named policy, this call would 429 too because both routes shared "SignUpStart".
        var verifyResponse = await client.PostAsJsonAsync("/api/auth/native/signup/verify-email",
            new SignUpVerifyEmailRequest("some-continuation-token", "000000"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task SignInStart_ReturnsTokensWithOid_WhenCredentialsValid()
    {
        var oid = Guid.NewGuid().ToString();
        var idToken = DevJwtTokenFactory.CreateToken(oid);
        var fake = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignInWithPassword = (_, _) => new NativeAuthTokenResult("access-abc", idToken, "refresh-abc", 3600)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/native/signin/start",
            new SignInStartRequest("jane@example.com", "P@ssw0rd123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.Equal(oid, body!.Oid);
        Assert.Equal("access-abc", body.AccessToken);
    }

    [Fact]
    public async Task SignInStart_Returns401_WhenPasswordIncorrect()
    {
        var fake = new FakeNativeAuthClient
        {
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignInWithPassword = (_, _) => throw new NativeAuthApiException("invalid_grant", "wrong password", null, null)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/native/signin/start",
            new SignInStartRequest("jane@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_FullRoundTrip_ReturnsTokens()
    {
        var oid = Guid.NewGuid().ToString();
        var idToken = DevJwtTokenFactory.CreateToken(oid);
        var fake = new FakeNativeAuthClient
        {
            OnResetPasswordStart = _ => new NativeAuthStepResult("start-token", "none", null, null, null),
            OnResetPasswordChallenge = _ => new NativeAuthStepResult("challenge-token", "oob", "email", "j***@example.com", 8),
            OnResetPasswordContinueWithOob = (_, _) => new NativeAuthStepResult("verified-token", "none", null, null, null),
            OnResetPasswordSubmit = (_, _) => new NativeAuthResetPasswordSubmitResult("submit-token", 0),
            OnResetPasswordPollCompletion = _ => new NativeAuthResetPasswordPollResult("succeeded", "after-password-token"),
            OnExchangeContinuationToken = _ => new NativeAuthTokenResult("access-xyz", idToken, "refresh-xyz", 3600)
        };
        using var factory = CreateFactory(fake);
        using var client = factory.CreateClient();

        var startResponse = await client.PostAsJsonAsync("/api/auth/native/password-reset/start",
            new PasswordResetStartRequest("jane@example.com"));
        var startBody = await startResponse.Content.ReadFromJsonAsync<OtpChallengeResponse>();

        var verifyResponse = await client.PostAsJsonAsync("/api/auth/native/password-reset/verify",
            new PasswordResetVerifyRequest(startBody!.ContinuationToken, "12345678"));
        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<PasswordResetVerifyResponse>();

        var completeResponse = await client.PostAsJsonAsync("/api/auth/native/password-reset/complete",
            new PasswordResetCompleteRequest(verifyBody!.ContinuationToken, "NewP@ssw0rd123"));

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var tokens = await completeResponse.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.Equal(oid, tokens!.Oid);
        Assert.Equal("access-xyz", tokens.AccessToken);
    }
}
