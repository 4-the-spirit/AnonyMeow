namespace AnonyMeow.Services.NativeAuth;

public interface IPasswordResetFlowService
{
    Task<NativeAuthStepResult> StartAsync(string email, CancellationToken cancellationToken = default);

    /// <returns>The continuation token to pass to <see cref="CompleteAsync"/>.</returns>
    Task<string> VerifyCodeAsync(string continuationToken, string code, CancellationToken cancellationToken = default);

    Task<NativeAuthTokenResult> CompleteAsync(string continuationToken, string newPassword, CancellationToken cancellationToken = default);
}
