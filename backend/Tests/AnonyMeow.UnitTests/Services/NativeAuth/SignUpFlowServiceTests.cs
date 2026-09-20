using AnonyMeow.Common.Exceptions;
using AnonyMeow.Services.NativeAuth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnonyMeow.UnitTests.Services.NativeAuth;

public class SignUpFlowServiceTests
{
    [Fact]
    public async Task StartAsync_ReturnsOtpChallenge_WhenTenantRequiresEmailVerification()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => new NativeAuthStepResult("start-token", "none", null, null, null),
            OnSignUpChallenge = _ => new NativeAuthStepResult("challenge-token", "oob", "email", "j***@example.com", 8)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        var result = await service.StartAsync("jane@example.com", "P@ssw0rd123");

        Assert.NotNull(result.OtpChallenge);
        Assert.Null(result.Tokens);
        Assert.Equal("challenge-token", result.OtpChallenge!.ContinuationToken);
        Assert.Equal(8, result.OtpChallenge.CodeLength);
    }

    [Fact]
    public async Task StartAsync_ReturnsTokens_WhenTenantSkipsEmailVerification()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => new NativeAuthStepResult("start-token", "none", null, null, null),
            OnSignUpChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignUpContinueWithPassword = (_, _) => new NativeAuthStepResult("password-ok-token", "none", null, null, null),
            OnExchangeContinuationToken = _ => new NativeAuthTokenResult("access", "id", "refresh", 3600)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        var result = await service.StartAsync("jane@example.com", "P@ssw0rd123");

        Assert.Null(result.OtpChallenge);
        Assert.NotNull(result.Tokens);
        Assert.Equal("access", result.Tokens!.AccessToken);
    }

    [Fact]
    public async Task StartAsync_Throws_EmailAlreadyRegistered_OnUserAlreadyExists_AndSignInFallbackFails()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => throw new NativeAuthApiException("user_already_exists", "already exists", null, null),
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignInWithPassword = (_, _) => throw new NativeAuthApiException("invalid_grant", "wrong password", null, null)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        await Assert.ThrowsAsync<NativeAuthEmailAlreadyRegisteredException>(
            () => service.StartAsync("jane@example.com", "wrong-password"));
    }

    [Fact]
    public async Task StartAsync_SignsInInstead_WhenUserAlreadyExists_AndPasswordMatches()
    {
        // Covers the common case where a prior sign-up attempt actually completed verification
        // server-side (so the tenant considers the email claimed) but this app's own flow failed
        // afterward — the user has no way to know that and reasonably tries "signing up" again.
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => throw new NativeAuthApiException("user_already_exists", "already exists", null, null),
            OnSignInInitiate = _ => new NativeAuthStepResult("initiate-token", "none", null, null, null),
            OnSignInChallenge = _ => new NativeAuthStepResult("challenge-token", "password", null, null, null),
            OnSignInWithPassword = (_, _) => new NativeAuthTokenResult("access", "id", "refresh", 3600)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        var result = await service.StartAsync("jane@example.com", "P@ssw0rd123");

        Assert.Null(result.OtpChallenge);
        Assert.NotNull(result.Tokens);
        Assert.Equal("access", result.Tokens!.AccessToken);
    }

    [Fact]
    public async Task StartAsync_Throws_Unavailable_WhenStartReturnsRedirect()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) => new NativeAuthStepResult("token", "redirect", null, null, null)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        await Assert.ThrowsAsync<NativeAuthUnavailableException>(
            () => service.StartAsync("jane@example.com", "P@ssw0rd123"));
    }

    [Fact]
    public async Task VerifyEmailAsync_ReturnsTokens_OnSuccessfulOtp()
    {
        // Password was already accepted in the /start call, so a verified OTP exchanges for tokens
        // directly — no separate password-continue step (see StartAsync_ReturnsTokens_WhenTenantSkipsEmailVerification
        // for the one case where a password-continue step legitimately happens).
        var client = new FakeNativeAuthClient
        {
            OnSignUpContinueWithOob = (_, _) => new NativeAuthStepResult("after-oob", "none", null, null, null),
            OnExchangeContinuationToken = _ => new NativeAuthTokenResult("access", "id", "refresh", 3600)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        var tokens = await service.VerifyEmailAsync("challenge-token", "12345678");

        Assert.Equal("access", tokens.AccessToken);
        Assert.Equal("refresh", tokens.RefreshToken);
    }

    [Fact]
    public async Task VerifyEmailAsync_Throws_InvalidCode_OnInvalidOobValue()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpContinueWithOob = (_, _) =>
                throw new NativeAuthApiException("invalid_grant", "wrong code", "invalid_oob_value", null)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        await Assert.ThrowsAsync<NativeAuthInvalidCodeException>(
            () => service.VerifyEmailAsync("challenge-token", "00000000"));
    }

    [Fact]
    public async Task VerifyEmailAsync_Throws_AttributesRequired_WhenUserFlowDemandsExtraAttributes()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpContinueWithOob = (_, _) => new NativeAuthStepResult("after-oob", "none", null, null, null),
            OnExchangeContinuationToken = _ =>
                throw new NativeAuthApiException(
                    "attributes_required", "attributes needed", null, "attrs-token", ["displayName"])
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        var ex = await Assert.ThrowsAsync<NativeAuthAttributesRequiredException>(
            () => service.VerifyEmailAsync("challenge-token", "12345678"));
        Assert.Contains("displayName", ex.Detail);
    }

    [Fact]
    public async Task StartAsync_Throws_WeakPassword_OnPasswordTooWeakSuberror()
    {
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) =>
                throw new NativeAuthApiException("invalid_grant", "New password is too weak", "password_too_weak", null)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        await Assert.ThrowsAsync<NativeAuthWeakPasswordException>(
            () => service.StartAsync("jane@example.com", "weak"));
    }

    [Fact]
    public async Task StartAsync_WeakPasswordMessage_DoesNotForwardTheTenantsRawDiagnosticText()
    {
        // Microsoft's error_description for password rejections is a raw diagnostic dump (trace
        // ID, correlation ID, timestamp) meant for logs, not end users — it must never reach the
        // client verbatim. See NativeAuthErrorMapping.DescribeWeakPassword.
        const string rawTenantDescription =
            "AADSTS50034: New password does not meet complexity requirements.\r\nTrace ID: abc\r\nCorrelation ID: def\r\nTimestamp: 2026-08-08";
        var client = new FakeNativeAuthClient
        {
            OnSignUpStart = (_, _) =>
                throw new NativeAuthApiException("invalid_grant", rawTenantDescription, "password_too_weak", null)
        };
        var service = new SignUpFlowService(client, new SignInFlowService(client), NullLogger<SignUpFlowService>.Instance);

        var ex = await Assert.ThrowsAsync<NativeAuthWeakPasswordException>(
            () => service.StartAsync("jane@example.com", "weak"));

        Assert.DoesNotContain("Trace ID", ex.Detail);
        Assert.DoesNotContain("Correlation ID", ex.Detail);
        Assert.DoesNotContain("AADSTS50034", ex.Detail);
    }

    [Fact]
    public async Task VerifyEmailAsync_LogsTheUnmappedError_WhenFallingBackToUnavailable()
    {
        // Previously this branch silently discarded the tenant's actual error code/description,
        // leaving every unmapped failure indistinguishable in production logs from any other 503 —
        // see Execution Log/2026-08-08-fix-signup-verify-email-unmapped-error-logging.md.
        var client = new FakeNativeAuthClient
        {
            OnSignUpContinueWithOob = (_, _) =>
                throw new NativeAuthApiException("unauthorized_client", "tenant rejected the request", "some_sub_error", null)
        };
        var recordingLogger = new RecordingLogger<SignUpFlowService>();
        var service = new SignUpFlowService(client, new SignInFlowService(client), recordingLogger);

        await Assert.ThrowsAsync<NativeAuthUnavailableException>(
            () => service.VerifyEmailAsync("challenge-token", "12345678"));

        var message = Assert.Single(recordingLogger.WarningMessages);
        Assert.Contains("signup/verify-email", message);
        Assert.Contains("unauthorized_client", message);
        Assert.Contains("some_sub_error", message);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> WarningMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningMessages.Add(formatter(state, exception));
            }
        }
    }
}
