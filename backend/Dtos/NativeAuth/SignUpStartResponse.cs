namespace AnonyMeow.Dtos.NativeAuth;

// NextStep is "verifyEmail" (OtpChallenge set) or "completed" (Tokens set) — the tenant's user
// flow config decides which one happens, see SignUpFlowService.StartAsync.
public record SignUpStartResponse(string NextStep, OtpChallengeResponse? OtpChallenge, AuthTokensResponse? Tokens);
