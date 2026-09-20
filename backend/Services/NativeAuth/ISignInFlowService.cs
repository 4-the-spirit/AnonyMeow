namespace AnonyMeow.Services.NativeAuth;

public interface ISignInFlowService
{
    Task<NativeAuthTokenResult> StartAsync(string email, string password, CancellationToken cancellationToken = default);
}
