using AnonyMeow.Services.NativeAuth;

namespace AnonyMeow.UnitTests.Services.NativeAuth;

/// <summary>
/// Hand-rolled fake (no mocking library in this codebase — see FriendshipServiceTests'
/// FakeNotificationDispatcher for the same convention). Each test wires up only the delegates the
/// flow under test actually calls; anything else throws so an unexpected call fails loudly.
/// </summary>
internal class FakeNativeAuthClient : INativeAuthClient
{
    public Func<string, string, NativeAuthStepResult>? OnSignUpStart { get; set; }
    public Func<string, NativeAuthStepResult>? OnSignUpChallenge { get; set; }
    public Func<string, string, NativeAuthStepResult>? OnSignUpContinueWithOob { get; set; }
    public Func<string, string, NativeAuthStepResult>? OnSignUpContinueWithPassword { get; set; }
    public Func<string, NativeAuthTokenResult>? OnExchangeContinuationToken { get; set; }
    public Func<string, NativeAuthStepResult>? OnSignInInitiate { get; set; }
    public Func<string, NativeAuthStepResult>? OnSignInChallenge { get; set; }
    public Func<string, string, NativeAuthTokenResult>? OnSignInWithPassword { get; set; }
    public Func<string, NativeAuthTokenResult>? OnRefreshToken { get; set; }
    public Func<string, NativeAuthStepResult>? OnResetPasswordStart { get; set; }
    public Func<string, NativeAuthStepResult>? OnResetPasswordChallenge { get; set; }
    public Func<string, string, NativeAuthStepResult>? OnResetPasswordContinueWithOob { get; set; }
    public Func<string, string, NativeAuthResetPasswordSubmitResult>? OnResetPasswordSubmit { get; set; }
    public Func<string, NativeAuthResetPasswordPollResult>? OnResetPasswordPollCompletion { get; set; }

    public Task<NativeAuthStepResult> SignUpStartAsync(string email, string password, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignUpStart, email, password));

    public Task<NativeAuthStepResult> SignUpChallengeAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignUpChallenge, continuationToken));

    public Task<NativeAuthStepResult> SignUpContinueWithOobAsync(string continuationToken, string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignUpContinueWithOob, continuationToken, code));

    public Task<NativeAuthStepResult> SignUpContinueWithPasswordAsync(string continuationToken, string password, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignUpContinueWithPassword, continuationToken, password));

    public Task<NativeAuthTokenResult> ExchangeContinuationTokenAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnExchangeContinuationToken, continuationToken));

    public Task<NativeAuthStepResult> SignInInitiateAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignInInitiate, email));

    public Task<NativeAuthStepResult> SignInChallengeAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignInChallenge, continuationToken));

    public Task<NativeAuthTokenResult> SignInWithPasswordAsync(string continuationToken, string password, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnSignInWithPassword, continuationToken, password));

    public Task<NativeAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnRefreshToken, refreshToken));

    public Task<NativeAuthStepResult> ResetPasswordStartAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnResetPasswordStart, email));

    public Task<NativeAuthStepResult> ResetPasswordChallengeAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnResetPasswordChallenge, continuationToken));

    public Task<NativeAuthStepResult> ResetPasswordContinueWithOobAsync(string continuationToken, string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnResetPasswordContinueWithOob, continuationToken, code));

    public Task<NativeAuthResetPasswordSubmitResult> ResetPasswordSubmitAsync(string continuationToken, string newPassword, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnResetPasswordSubmit, continuationToken, newPassword));

    public Task<NativeAuthResetPasswordPollResult> ResetPasswordPollCompletionAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoke(OnResetPasswordPollCompletion, continuationToken));

    private static TResult Invoke<TArg, TResult>(Func<TArg, TResult>? handler, TArg arg) =>
        handler is null ? throw new InvalidOperationException("Unexpected call: no fake handler configured.") : handler(arg);

    private static TResult Invoke<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult>? handler, TArg1 arg1, TArg2 arg2) =>
        handler is null ? throw new InvalidOperationException("Unexpected call: no fake handler configured.") : handler(arg1, arg2);
}
