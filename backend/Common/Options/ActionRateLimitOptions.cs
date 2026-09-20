namespace AnonyMeow.Common.Options;

public class RateLimitPolicySettings
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

// Named per-action policies layered on top of the Phase 0 global baseline limiter — thresholds
// start conservative and iterate, per the parent plan.
public class ActionRateLimitOptions
{
    public const string SectionName = "ActionRateLimits";

    public RateLimitPolicySettings CreatePost { get; set; } = new() { PermitLimit = 5, WindowSeconds = 300 };
    public RateLimitPolicySettings CreateComment { get; set; } = new() { PermitLimit = 15, WindowSeconds = 300 };
    public RateLimitPolicySettings SendMessage { get; set; } = new() { PermitLimit = 30, WindowSeconds = 300 };
    public RateLimitPolicySettings Vote { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
    public RateLimitPolicySettings Report { get; set; } = new() { PermitLimit = 10, WindowSeconds = 300 };
    public RateLimitPolicySettings ImageUpload { get; set; } = new() { PermitLimit = 30, WindowSeconds = 86400 };

    // Unauthenticated, public, credential-guessing surface — kept conservative. SignUpStart and
    // SignUpVerify are separate buckets (rather than one shared "signup flow" bucket) so that an
    // OTP retry/resend during verification can't also starve out someone else's ability to start
    // a new signup, and vice versa.
    public RateLimitPolicySettings SignUpStart { get; set; } = new() { PermitLimit = 5, WindowSeconds = 600 };
    public RateLimitPolicySettings SignUpVerify { get; set; } = new() { PermitLimit = 5, WindowSeconds = 600 };
    public RateLimitPolicySettings SignInStart { get; set; } = new() { PermitLimit = 5, WindowSeconds = 600 };
    public RateLimitPolicySettings PasswordReset { get; set; } = new() { PermitLimit = 5, WindowSeconds = 600 };
    public RateLimitPolicySettings RefreshToken { get; set; } = new() { PermitLimit = 20, WindowSeconds = 300 };
}
