namespace AnonyMeow.Services.NativeAuth;

/// <summary>Exactly one of OtpChallenge/Tokens is set: OtpChallenge when the tenant's user flow
/// requires email verification (the password was already submitted and accepted in the /start
/// call at this point — verifying the OTP is the only thing left before tokens are issued), Tokens
/// on the (less common, but tenant-config-dependent) path where sign-up completes without an OTP
/// step.</summary>
public record SignUpStartResult(NativeAuthStepResult? OtpChallenge, NativeAuthTokenResult? Tokens);

public interface ISignUpFlowService
{
    Task<SignUpStartResult> StartAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<NativeAuthTokenResult> VerifyEmailAsync(
        string continuationToken, string code, CancellationToken cancellationToken = default);
}
