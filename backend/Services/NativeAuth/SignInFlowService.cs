using AnonyMeow.Common.Exceptions;

namespace AnonyMeow.Services.NativeAuth;

public class SignInFlowService(INativeAuthClient client) : ISignInFlowService
{
    public async Task<NativeAuthTokenResult> StartAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        NativeAuthStepResult initiate;
        try
        {
            initiate = await client.SignInInitiateAsync(email, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapInitiateError(ex, email);
        }

        if (initiate.ChallengeType == "redirect")
        {
            throw new NativeAuthUnavailableException();
        }

        NativeAuthStepResult challenge;
        try
        {
            challenge = await client.SignInChallengeAsync(initiate.ContinuationToken!, cancellationToken);
        }
        catch (NativeAuthApiException)
        {
            throw new NativeAuthUnavailableException();
        }

        // This tenant's user flow is "Email with password" only (no OTP/MFA sign-in configured) —
        // anything other than a password challenge here means an unexpected tenant/config state,
        // not something this app's UI currently has a step for.
        if (challenge.ChallengeType != "password")
        {
            throw new NativeAuthUnavailableException();
        }

        try
        {
            return await client.SignInWithPasswordAsync(challenge.ContinuationToken!, password, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapTokenError(ex);
        }
    }

    private static Exception MapInitiateError(NativeAuthApiException ex, string email) => ex.Error switch
    {
        "user_not_found" => new NativeAuthAccountNotFoundException(email),
        _ => new NativeAuthUnavailableException()
    };

    private static Exception MapTokenError(NativeAuthApiException ex) => ex.Error switch
    {
        "invalid_grant" => new NativeAuthInvalidCredentialsException(),
        _ => new NativeAuthUnavailableException()
    };
}
