namespace AnonyMeow.Common.Exceptions;

// Raw, untranslated error from Microsoft's native authentication REST API — thrown by
// NativeAuthClient, caught and translated into a specific ApiException subclass by the
// SignUpFlowService/SignInFlowService/PasswordResetFlowService that called it. Deliberately not
// an ApiException itself: NativeAuthClient has no business deciding HTTP status codes or
// user-facing copy, that's the orchestrating flow service's job.
public class NativeAuthApiException(
    string error,
    string? errorDescription,
    string? subError,
    string? continuationToken,
    IReadOnlyList<string>? requiredAttributes = null)
    : Exception(errorDescription ?? error)
{
    public string Error { get; } = error;
    public string? SubError { get; } = subError;
    public string? ContinuationToken { get; } = continuationToken;
    public IReadOnlyList<string>? RequiredAttributes { get; } = requiredAttributes;
}
