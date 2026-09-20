using AnonyMeow.Common.Exceptions;
using AnonyMeow.Services.NativeAuth;

namespace AnonyMeow.UnitTests.Services.NativeAuth;

public class PasswordResetFlowServiceTests
{
    [Fact]
    public async Task StartAsync_ReturnsOtpChallenge_OnSuccess()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordStart = _ => new NativeAuthStepResult("start-token", "none", null, null, null),
            OnResetPasswordChallenge = _ => new NativeAuthStepResult("challenge-token", "oob", "email", "j***@example.com", 8)
        };
        var service = new PasswordResetFlowService(client);

        var challenge = await service.StartAsync("jane@example.com");

        Assert.Equal("challenge-token", challenge.ContinuationToken);
        Assert.Equal(8, challenge.CodeLength);
    }

    [Fact]
    public async Task StartAsync_Throws_AccountNotFound_OnUserNotFound()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordStart = _ => throw new NativeAuthApiException("user_not_found", "no such user", null, null)
        };
        var service = new PasswordResetFlowService(client);

        await Assert.ThrowsAsync<NativeAuthAccountNotFoundException>(() => service.StartAsync("jane@example.com"));
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsContinuationToken_OnSuccess()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordContinueWithOob = (_, _) => new NativeAuthStepResult("verified-token", "none", null, null, null)
        };
        var service = new PasswordResetFlowService(client);

        var token = await service.VerifyCodeAsync("challenge-token", "12345678");

        Assert.Equal("verified-token", token);
    }

    [Fact]
    public async Task VerifyCodeAsync_Throws_InvalidCode_OnInvalidOobValue()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordContinueWithOob = (_, _) =>
                throw new NativeAuthApiException("invalid_grant", "wrong code", "invalid_oob_value", null)
        };
        var service = new PasswordResetFlowService(client);

        await Assert.ThrowsAsync<NativeAuthInvalidCodeException>(() => service.VerifyCodeAsync("challenge-token", "00000000"));
    }

    [Fact]
    public async Task CompleteAsync_ReturnsTokens_WhenPollingSucceedsImmediately()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordSubmit = (_, _) => new NativeAuthResetPasswordSubmitResult("submit-token", 0),
            OnResetPasswordPollCompletion = _ => new NativeAuthResetPasswordPollResult("succeeded", "ready-token"),
            OnExchangeContinuationToken = token =>
            {
                Assert.Equal("ready-token", token);
                return new NativeAuthTokenResult("access", "id", "refresh", 3600);
            }
        };
        var service = new PasswordResetFlowService(client);

        var tokens = await service.CompleteAsync("verified-token", "NewP@ssw0rd123");

        Assert.Equal("access", tokens.AccessToken);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsTokens_AfterPollingInProgressThenSucceeded()
    {
        var pollCount = 0;
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordSubmit = (_, _) => new NativeAuthResetPasswordSubmitResult("submit-token", 0),
            OnResetPasswordPollCompletion = _ =>
            {
                pollCount++;
                return pollCount < 3
                    ? new NativeAuthResetPasswordPollResult("in_progress", "submit-token")
                    : new NativeAuthResetPasswordPollResult("succeeded", "ready-token");
            },
            OnExchangeContinuationToken = _ => new NativeAuthTokenResult("access", "id", "refresh", 3600)
        };
        var service = new PasswordResetFlowService(client);

        var tokens = await service.CompleteAsync("verified-token", "NewP@ssw0rd123");

        Assert.Equal(3, pollCount);
        Assert.Equal("access", tokens.AccessToken);
    }

    [Fact]
    public async Task CompleteAsync_Throws_Unavailable_WhenPollingFails()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordSubmit = (_, _) => new NativeAuthResetPasswordSubmitResult("submit-token", 0),
            OnResetPasswordPollCompletion = _ => new NativeAuthResetPasswordPollResult("failed", null)
        };
        var service = new PasswordResetFlowService(client);

        await Assert.ThrowsAsync<NativeAuthUnavailableException>(() => service.CompleteAsync("verified-token", "NewP@ssw0rd123"));
    }

    [Fact]
    public async Task CompleteAsync_Throws_WeakPassword_OnPasswordTooWeakSuberror()
    {
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordSubmit = (_, _) =>
                throw new NativeAuthApiException("invalid_grant", "too weak", "password_too_weak", null)
        };
        var service = new PasswordResetFlowService(client);

        await Assert.ThrowsAsync<NativeAuthWeakPasswordException>(() => service.CompleteAsync("verified-token", "weak"));
    }

    [Fact]
    public async Task CompleteAsync_WeakPasswordMessage_DoesNotForwardTheTenantsRawDiagnosticText()
    {
        // Same rationale as SignUpFlowServiceTests' equivalent test: Microsoft's error_description
        // is a raw diagnostic dump, not user-facing copy — see NativeAuthErrorMapping.DescribeWeakPassword.
        const string rawTenantDescription =
            "AADSTS50034: New password does not meet complexity requirements.\r\nTrace ID: abc\r\nCorrelation ID: def\r\nTimestamp: 2026-08-08";
        var client = new FakeNativeAuthClient
        {
            OnResetPasswordSubmit = (_, _) =>
                throw new NativeAuthApiException("invalid_grant", rawTenantDescription, "password_too_weak", null)
        };
        var service = new PasswordResetFlowService(client);

        var ex = await Assert.ThrowsAsync<NativeAuthWeakPasswordException>(
            () => service.CompleteAsync("verified-token", "weak"));

        Assert.DoesNotContain("Trace ID", ex.Detail);
        Assert.DoesNotContain("Correlation ID", ex.Detail);
        Assert.DoesNotContain("AADSTS50034", ex.Detail);
    }
}
