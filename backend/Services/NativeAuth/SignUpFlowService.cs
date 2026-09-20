using AnonyMeow.Common.Exceptions;

namespace AnonyMeow.Services.NativeAuth;

public class SignUpFlowService(
    INativeAuthClient client, ISignInFlowService signInFlowService, ILogger<SignUpFlowService> logger) : ISignUpFlowService
{
    public async Task<SignUpStartResult> StartAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        NativeAuthStepResult start;
        try
        {
            start = await client.SignUpStartAsync(email, password, cancellationToken);
        }
        catch (NativeAuthApiException ex) when (ex.Error == "user_already_exists")
        {
            // The tenant claims an email as soon as /signup/start first succeeds, even for an
            // account that never completed OTP verification — Microsoft's native-auth API has no
            // documented way to resume that pending signup once its original continuation token is
            // lost (e.g. the user closed the tab). So "sign up" on an already-claimed email is a
            // routine path, not just a conflict: try signing in with the same credentials first: if
            // they match, the account was actually completed already (e.g. a prior verification
            // succeeded server-side but this app failed afterward during token exchange — see
            // Execution Log/2026-08-08-fix-native-signup-otp-token-exchange.md) and the user ends up
            // signed in either way. Any failure here (wrong password, unverified account, etc.)
            // falls through to the original 409 — this is a best-effort improvement, never worse
            // than the conflict error that existed before it.
            try
            {
                var tokens = await signInFlowService.StartAsync(email, password, cancellationToken);
                return new SignUpStartResult(null, tokens);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested is false)
            {
                throw MapStartError(ex, email);
            }
        }
        catch (NativeAuthApiException ex)
        {
            throw MapStartError(ex, email);
        }

        if (start.ChallengeType == "redirect")
        {
            throw new NativeAuthUnavailableException();
        }

        NativeAuthStepResult challenge;
        try
        {
            challenge = await client.SignUpChallengeAsync(start.ContinuationToken!, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }

        return challenge.ChallengeType switch
        {
            "oob" => new SignUpStartResult(challenge, null),
            // Tenant configured without a required email-verification step — the password was
            // already sent in the start call, so this just confirms it and exchanges for tokens.
            "password" => new SignUpStartResult(
                null, await ContinueWithPasswordThenExchangeAsync(challenge.ContinuationToken!, password, cancellationToken)),
            _ => throw new NativeAuthUnavailableException()
        };
    }

    public async Task<NativeAuthTokenResult> VerifyEmailAsync(
        string continuationToken, string code, CancellationToken cancellationToken = default)
    {
        NativeAuthStepResult afterOob;
        try
        {
            afterOob = await client.SignUpContinueWithOobAsync(continuationToken, code, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }

        // The password was already submitted (and accepted) in the /start call, so once the OTP is
        // verified there's nothing left to confirm — exchange straight for tokens. Resubmitting the
        // password here via another /continue call is not a step Microsoft's protocol expects at
        // this point and was rejected with an unmapped error, which is why sign-up used to fail with
        // a generic "unable to create account" error after a correct OTP code.
        try
        {
            return await client.ExchangeContinuationTokenAsync(afterOob.ContinuationToken!, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }
    }

    private async Task<NativeAuthTokenResult> ContinueWithPasswordThenExchangeAsync(
        string continuationToken, string password, CancellationToken cancellationToken)
    {
        NativeAuthStepResult afterPassword;
        try
        {
            afterPassword = await client.SignUpContinueWithPasswordAsync(continuationToken, password, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }

        try
        {
            return await client.ExchangeContinuationTokenAsync(afterPassword.ContinuationToken!, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }
    }

    private Exception MapStartError(NativeAuthApiException ex, string email) => ex.Error switch
    {
        "user_already_exists" => new NativeAuthEmailAlreadyRegisteredException(email),
        "invalid_grant" when NativeAuthErrorMapping.IsWeakPasswordSubError(ex.SubError) =>
            new NativeAuthWeakPasswordException(NativeAuthErrorMapping.DescribeWeakPassword(ex.SubError)),
        _ => LogAndMakeUnavailable("signup/start", ex)
    };

    private Exception MapContinueError(NativeAuthApiException ex) => ex.Error switch
    {
        "attributes_required" => new NativeAuthAttributesRequiredException(ex.RequiredAttributes ?? []),
        "expired_token" => new NativeAuthInvalidCodeException(),
        "invalid_grant" when ex.SubError == "invalid_oob_value" => new NativeAuthInvalidCodeException(),
        "invalid_grant" when NativeAuthErrorMapping.IsWeakPasswordSubError(ex.SubError) =>
            new NativeAuthWeakPasswordException(NativeAuthErrorMapping.DescribeWeakPassword(ex.SubError)),
        _ => LogAndMakeUnavailable("signup/verify-email", ex)
    };

    // Every other branch above maps to a specific, expected Microsoft error code. Landing here
    // means the tenant returned something none of today's flows anticipate — previously that was
    // swallowed into an opaque 503 with zero trace, which is exactly why the last several fixes to
    // this file could only be verified against Microsoft's docs, never against what actually went
    // wrong in production. Error/SubError/Message are protocol-level diagnostic codes, not PII.
    private NativeAuthUnavailableException LogAndMakeUnavailable(string flowStep, NativeAuthApiException ex)
    {
        logger.LogWarning(
            "Native auth {FlowStep} received an unmapped error from the tenant, surfacing as 503: {Error}/{SubError} — {Description}",
            flowStep, ex.Error, ex.SubError, ex.Message);
        return new NativeAuthUnavailableException();
    }
}
