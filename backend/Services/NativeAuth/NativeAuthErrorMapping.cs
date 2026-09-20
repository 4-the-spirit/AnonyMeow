namespace AnonyMeow.Services.NativeAuth;

// Shared between SignUpFlowService and PasswordResetFlowService, both of which submit a new
// password and need to recognize the same set of "invalid_grant" suberrors Microsoft returns for
// a rejected password.
internal static class NativeAuthErrorMapping
{
    public static bool IsWeakPasswordSubError(string? subError) => subError is
        "password_too_weak" or "password_too_short" or "password_too_long" or
        "password_recently_used" or "password_banned" or "password_is_invalid";

    // Microsoft's error_description for these is a raw diagnostic dump (trace ID, correlation ID,
    // timestamp) meant for logs, not end users, so it's never forwarded to the client as-is —
    // this maps each recognized suberror to a clean, actionable message instead. Wording mirrors
    // the client-side rule in frontend/src/lib/validation.ts (newPasswordSchema) and the policy
    // documented at https://learn.microsoft.com/entra/identity/authentication/concept-sspr-policy.
    public static string DescribeWeakPassword(string? subError) => subError switch
    {
        "password_too_short" => "Password must be at least 8 characters long.",
        "password_too_long" => "Password must be no more than 256 characters long.",
        "password_recently_used" => "You've used that password recently. Please choose a different one.",
        "password_banned" => "That password is too common. Please choose a different one.",
        _ => "Password must be 8-256 characters and include at least 3 of: uppercase letters, lowercase letters, numbers, and symbols.",
    };
}
