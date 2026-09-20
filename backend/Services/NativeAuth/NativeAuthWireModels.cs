using System.Text.Json.Serialization;

namespace AnonyMeow.Services.NativeAuth;

// Raw wire shapes for Microsoft's native authentication REST API (snake_case, form-encoded
// requests / JSON responses) — internal to NativeAuthClient, never exposed past it. Explicit
// JsonPropertyName attributes rather than relying on ambient naming policy, since this is a
// hand-rolled HttpClient call with its own JsonSerializerOptions, unrelated to this app's own
// Minimal API request/response serialization.

internal record NativeAuthSuccessEnvelope(
    [property: JsonPropertyName("continuation_token")] string? ContinuationToken,
    [property: JsonPropertyName("challenge_type")] string? ChallengeType,
    [property: JsonPropertyName("challenge_channel")] string? ChallengeChannel,
    [property: JsonPropertyName("challenge_target_label")] string? ChallengeTargetLabel,
    [property: JsonPropertyName("code_length")] int? CodeLength);

internal record NativeAuthErrorEnvelope(
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription,
    [property: JsonPropertyName("suberror")] string? SubError,
    [property: JsonPropertyName("continuation_token")] string? ContinuationToken,
    [property: JsonPropertyName("required_attributes")] List<RequiredAttributeWire>? RequiredAttributes);

internal record RequiredAttributeWire([property: JsonPropertyName("name")] string Name);

internal record NativeAuthTokenEnvelope(
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("id_token")] string? IdToken);

internal record NativeAuthResetPasswordSubmitEnvelope(
    [property: JsonPropertyName("continuation_token")] string? ContinuationToken,
    [property: JsonPropertyName("poll_interval")] int? PollIntervalSeconds);

internal record NativeAuthResetPasswordPollEnvelope(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("continuation_token")] string? ContinuationToken);
