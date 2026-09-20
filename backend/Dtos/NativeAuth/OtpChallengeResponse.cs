namespace AnonyMeow.Dtos.NativeAuth;

public record OtpChallengeResponse(string ContinuationToken, int? CodeLength, string? MaskedEmail);
