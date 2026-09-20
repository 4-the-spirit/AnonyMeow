namespace AnonyMeow.Common.Exceptions;

// Microsoft responded with challenge_type "redirect" (or invalid_client/nativeauthapi_disabled) —
// native authentication isn't usable for this request (e.g. a Conditional Access policy forced a
// browser redirect, or the app registration's native-auth flows got disabled). There's no
// hosted-popup fallback anymore in this app, so this is a hard failure, not a step to recover from.
public class NativeAuthUnavailableException()
    : ApiException(
        StatusCodes.Status503ServiceUnavailable,
        "Sign-In Temporarily Unavailable",
        "Sign-in is temporarily unavailable. Please try again shortly.");
