namespace AnonyMeow.Services.NativeAuth;

/// <summary>A single step's result: either a further challenge to satisfy (ContinuationToken +
/// ChallengeType set, Tokens null) or, for the token-exchange calls, the final tokens.</summary>
public record NativeAuthStepResult(
    string? ContinuationToken,
    string ChallengeType,
    string? ChallengeChannel,
    string? MaskedTarget,
    int? CodeLength);

public record NativeAuthTokenResult(
    string AccessToken,
    string IdToken,
    string? RefreshToken,
    int ExpiresIn);

/// <summary>Result of submitting a new password during password reset — the change is applied
/// asynchronously on Microsoft's side, so this only returns a token to poll for completion with.</summary>
public record NativeAuthResetPasswordSubmitResult(string ContinuationToken, int PollIntervalSeconds);

/// <summary>Result of polling for password-reset completion. Status is one of "in_progress",
/// "succeeded", or "failed"; ContinuationToken (when present) supersedes the one that was polled
/// with for the next call (another poll, or the final token exchange once succeeded).</summary>
public record NativeAuthResetPasswordPollResult(string Status, string? ContinuationToken);

/// <summary>
/// Thin, 1:1 wrapper over Microsoft Entra External ID's native authentication REST API
/// (see https://learn.microsoft.com/entra/identity-platform/reference-native-authentication-api).
/// Every method either returns a step/token result or throws NativeAuthApiException — no
/// business-logic interpretation happens here, that's SignUpFlowService/SignInFlowService/
/// PasswordResetFlowService's job. This is the first HttpClient usage in this codebase; nothing
/// else here needs raw outbound HTTP.
/// </summary>
public interface INativeAuthClient
{
    Task<NativeAuthStepResult> SignUpStartAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> SignUpChallengeAsync(string continuationToken, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> SignUpContinueWithOobAsync(string continuationToken, string code, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> SignUpContinueWithPasswordAsync(string continuationToken, string password, CancellationToken cancellationToken = default);

    Task<NativeAuthTokenResult> ExchangeContinuationTokenAsync(string continuationToken, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> SignInInitiateAsync(string email, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> SignInChallengeAsync(string continuationToken, CancellationToken cancellationToken = default);

    Task<NativeAuthTokenResult> SignInWithPasswordAsync(string continuationToken, string password, CancellationToken cancellationToken = default);

    Task<NativeAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> ResetPasswordStartAsync(string email, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> ResetPasswordChallengeAsync(string continuationToken, CancellationToken cancellationToken = default);

    Task<NativeAuthStepResult> ResetPasswordContinueWithOobAsync(string continuationToken, string code, CancellationToken cancellationToken = default);

    Task<NativeAuthResetPasswordSubmitResult> ResetPasswordSubmitAsync(string continuationToken, string newPassword, CancellationToken cancellationToken = default);

    Task<NativeAuthResetPasswordPollResult> ResetPasswordPollCompletionAsync(string continuationToken, CancellationToken cancellationToken = default);
}
