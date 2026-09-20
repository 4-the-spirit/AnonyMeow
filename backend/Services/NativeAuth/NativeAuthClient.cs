using System.Net.Http.Json;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Common.Options;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services.NativeAuth;

public class NativeAuthClient(HttpClient httpClient, IOptions<NativeAuthOptions> options) : INativeAuthClient
{
    private string ClientId => options.Value.ClientId;

    public Task<NativeAuthStepResult> SignUpStartAsync(string email, string password, CancellationToken cancellationToken = default) =>
        PostStepAsync("signup/v1.0/start", new Dictionary<string, string>
        {
            ["username"] = email,
            ["password"] = password,
            ["challenge_type"] = "oob password redirect"
        }, cancellationToken);

    public Task<NativeAuthStepResult> SignUpChallengeAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        PostStepAsync("signup/v1.0/challenge", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["challenge_type"] = "oob password redirect"
        }, cancellationToken);

    public Task<NativeAuthStepResult> SignUpContinueWithOobAsync(string continuationToken, string code, CancellationToken cancellationToken = default) =>
        PostStepAsync("signup/v1.0/continue", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["grant_type"] = "oob",
            ["oob"] = code
        }, cancellationToken);

    public Task<NativeAuthStepResult> SignUpContinueWithPasswordAsync(string continuationToken, string password, CancellationToken cancellationToken = default) =>
        PostStepAsync("signup/v1.0/continue", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["grant_type"] = "password",
            ["password"] = password
        }, cancellationToken);

    public Task<NativeAuthTokenResult> ExchangeContinuationTokenAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        PostTokenAsync("oauth2/v2.0/token", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["grant_type"] = "continuation_token",
            ["scope"] = "openid profile offline_access"
        }, cancellationToken);

    public Task<NativeAuthStepResult> SignInInitiateAsync(string email, CancellationToken cancellationToken = default) =>
        PostStepAsync("oauth2/v2.0/initiate", new Dictionary<string, string>
        {
            ["username"] = email,
            ["challenge_type"] = "password redirect"
        }, cancellationToken);

    public Task<NativeAuthStepResult> SignInChallengeAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        PostStepAsync("oauth2/v2.0/challenge", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["challenge_type"] = "password redirect"
        }, cancellationToken);

    public Task<NativeAuthTokenResult> SignInWithPasswordAsync(string continuationToken, string password, CancellationToken cancellationToken = default) =>
        PostTokenAsync("oauth2/v2.0/token", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["grant_type"] = "password",
            ["password"] = password,
            ["scope"] = "openid profile offline_access"
        }, cancellationToken);

    public Task<NativeAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        PostTokenAsync("oauth2/v2.0/token", new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = "openid profile offline_access"
        }, cancellationToken);

    public Task<NativeAuthStepResult> ResetPasswordStartAsync(string email, CancellationToken cancellationToken = default) =>
        PostStepAsync("resetpassword/v1.0/start", new Dictionary<string, string>
        {
            ["username"] = email,
            ["challenge_type"] = "oob redirect"
        }, cancellationToken);

    public Task<NativeAuthStepResult> ResetPasswordChallengeAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        PostStepAsync("resetpassword/v1.0/challenge", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["challenge_type"] = "oob redirect"
        }, cancellationToken);

    public Task<NativeAuthStepResult> ResetPasswordContinueWithOobAsync(string continuationToken, string code, CancellationToken cancellationToken = default) =>
        PostStepAsync("resetpassword/v1.0/continue", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["grant_type"] = "oob",
            ["oob"] = code
        }, cancellationToken);

    // Not a /continue call, unlike every other step here — Microsoft's API uses a dedicated
    // /submit endpoint with a bare `new_password` field (no grant_type) for this step, and applies
    // the change asynchronously, returning a token to poll for completion with rather than a
    // normal step/token result. See ResetPasswordPollCompletionAsync and
    // https://learn.microsoft.com/entra/identity-platform/reference-native-authentication-api.
    public async Task<NativeAuthResetPasswordSubmitResult> ResetPasswordSubmitAsync(
        string continuationToken, string newPassword, CancellationToken cancellationToken = default)
    {
        var envelope = await PostAsync<NativeAuthResetPasswordSubmitEnvelope>("resetpassword/v1.0/submit", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken,
            ["new_password"] = newPassword
        }, cancellationToken);

        if (string.IsNullOrEmpty(envelope.ContinuationToken))
        {
            throw new NativeAuthApiException("invalid_response", "Submit response was missing a continuation token.", null, null);
        }

        return new NativeAuthResetPasswordSubmitResult(envelope.ContinuationToken, envelope.PollIntervalSeconds ?? 2);
    }

    public async Task<NativeAuthResetPasswordPollResult> ResetPasswordPollCompletionAsync(
        string continuationToken, CancellationToken cancellationToken = default)
    {
        var envelope = await PostAsync<NativeAuthResetPasswordPollEnvelope>("resetpassword/v1.0/poll_completion", new Dictionary<string, string>
        {
            ["continuation_token"] = continuationToken
        }, cancellationToken);

        if (string.IsNullOrEmpty(envelope.Status))
        {
            throw new NativeAuthApiException("invalid_response", "Poll response was missing a status.", null, null);
        }

        return new NativeAuthResetPasswordPollResult(envelope.Status, envelope.ContinuationToken);
    }

    private async Task<NativeAuthStepResult> PostStepAsync(
        string relativePath, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var envelope = await PostAsync<NativeAuthSuccessEnvelope>(relativePath, parameters, cancellationToken);
        return new NativeAuthStepResult(
            envelope.ContinuationToken,
            envelope.ChallengeType ?? "none",
            envelope.ChallengeChannel,
            envelope.ChallengeTargetLabel,
            envelope.CodeLength);
    }

    private async Task<NativeAuthTokenResult> PostTokenAsync(
        string relativePath, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var envelope = await PostAsync<NativeAuthTokenEnvelope>(relativePath, parameters, cancellationToken);
        if (string.IsNullOrEmpty(envelope.AccessToken) || string.IsNullOrEmpty(envelope.IdToken))
        {
            throw new NativeAuthApiException("invalid_response", "Token response was missing required fields.", null, null);
        }

        return new NativeAuthTokenResult(envelope.AccessToken, envelope.IdToken, envelope.RefreshToken, envelope.ExpiresIn);
    }

    private async Task<T> PostAsync<T>(string relativePath, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        parameters["client_id"] = ClientId;
        using var content = new FormUrlEncodedContent(parameters);
        using var response = await httpClient.PostAsync(relativePath, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var success = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return success ?? throw new NativeAuthApiException("invalid_response", "Empty response body.", null, null);
        }

        var error = await response.Content.ReadFromJsonAsync<NativeAuthErrorEnvelope>(cancellationToken: cancellationToken);
        var requiredAttributes = error?.RequiredAttributes?.Select(a => a.Name).ToList();
        throw new NativeAuthApiException(
            error?.Error ?? "unknown_error",
            error?.ErrorDescription,
            error?.SubError,
            error?.ContinuationToken,
            requiredAttributes);
    }
}
