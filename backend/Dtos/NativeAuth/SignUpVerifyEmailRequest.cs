namespace AnonyMeow.Dtos.NativeAuth;

public record SignUpVerifyEmailRequest(string ContinuationToken, string Code);
