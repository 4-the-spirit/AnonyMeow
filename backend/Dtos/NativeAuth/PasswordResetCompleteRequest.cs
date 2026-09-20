namespace AnonyMeow.Dtos.NativeAuth;

public record PasswordResetCompleteRequest(string ContinuationToken, string NewPassword);
