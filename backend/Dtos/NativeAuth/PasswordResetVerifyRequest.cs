namespace AnonyMeow.Dtos.NativeAuth;

public record PasswordResetVerifyRequest(string ContinuationToken, string Code);
