namespace AnonyMeow.Common.Exceptions;

// Mirrors the existing dev sign-in's "No account found with that username" precedent
// (AuthPage.tsx / devTokenAdapter's 404 handling) — this app already reveals account existence
// on sign-in, so staying consistent here rather than introducing stricter (and inconsistent)
// anti-enumeration behavior just for this flow.
public class NativeAuthAccountNotFoundException(string email)
    : ApiException(
        StatusCodes.Status404NotFound,
        "Account Not Found",
        $"No account found with the email '{email}'.");
