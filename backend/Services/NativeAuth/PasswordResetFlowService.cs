using AnonyMeow.Common.Exceptions;

namespace AnonyMeow.Services.NativeAuth;

public class PasswordResetFlowService(INativeAuthClient client) : IPasswordResetFlowService
{
    // Bounds how long CompleteAsync can block polling Microsoft for the async password-change
    // to land before giving up — at the 5s-clamped max delay below, that's up to ~50s.
    private const int MaxPollAttempts = 10;

    public async Task<NativeAuthStepResult> StartAsync(string email, CancellationToken cancellationToken = default)
    {
        NativeAuthStepResult start;
        try
        {
            start = await client.ResetPasswordStartAsync(email, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapStartError(ex, email);
        }

        if (start.ChallengeType == "redirect")
        {
            throw new NativeAuthUnavailableException();
        }

        try
        {
            var challenge = await client.ResetPasswordChallengeAsync(start.ContinuationToken!, cancellationToken);
            if (challenge.ChallengeType != "oob")
            {
                throw new NativeAuthUnavailableException();
            }

            return challenge;
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }
    }

    public async Task<string> VerifyCodeAsync(string continuationToken, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await client.ResetPasswordContinueWithOobAsync(continuationToken, code, cancellationToken);
            return result.ContinuationToken!;
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }
    }

    public async Task<NativeAuthTokenResult> CompleteAsync(
        string continuationToken, string newPassword, CancellationToken cancellationToken = default)
    {
        NativeAuthResetPasswordSubmitResult submitted;
        try
        {
            submitted = await client.ResetPasswordSubmitAsync(continuationToken, newPassword, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }

        var readyToken = await PollUntilCompleteAsync(submitted, cancellationToken);

        try
        {
            return await client.ExchangeContinuationTokenAsync(readyToken, cancellationToken);
        }
        catch (NativeAuthApiException ex)
        {
            throw MapContinueError(ex);
        }
    }

    private async Task<string> PollUntilCompleteAsync(
        NativeAuthResetPasswordSubmitResult submitted, CancellationToken cancellationToken)
    {
        var continuationToken = submitted.ContinuationToken;
        var delay = TimeSpan.FromSeconds(Math.Clamp(submitted.PollIntervalSeconds, 1, 5));

        for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            await Task.Delay(delay, cancellationToken);

            NativeAuthResetPasswordPollResult poll;
            try
            {
                poll = await client.ResetPasswordPollCompletionAsync(continuationToken, cancellationToken);
            }
            catch (NativeAuthApiException ex)
            {
                throw MapContinueError(ex);
            }

            switch (poll.Status)
            {
                case "succeeded":
                    return poll.ContinuationToken ?? continuationToken;
                case "in_progress":
                    continuationToken = poll.ContinuationToken ?? continuationToken;
                    continue;
                default:
                    throw new NativeAuthUnavailableException();
            }
        }

        throw new NativeAuthUnavailableException();
    }

    private static Exception MapStartError(NativeAuthApiException ex, string email) => ex.Error switch
    {
        "user_not_found" => new NativeAuthAccountNotFoundException(email),
        _ => new NativeAuthUnavailableException()
    };

    private static Exception MapContinueError(NativeAuthApiException ex) => ex.Error switch
    {
        "expired_token" => new NativeAuthInvalidCodeException(),
        "invalid_grant" when ex.SubError == "invalid_oob_value" => new NativeAuthInvalidCodeException(),
        "invalid_grant" when NativeAuthErrorMapping.IsWeakPasswordSubError(ex.SubError) =>
            new NativeAuthWeakPasswordException(NativeAuthErrorMapping.DescribeWeakPassword(ex.SubError)),
        _ => new NativeAuthUnavailableException()
    };
}
