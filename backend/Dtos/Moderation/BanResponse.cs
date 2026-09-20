namespace AnonyMeow.Dtos.Moderation;

public record BanResponse(string Username, string Reason, DateTimeOffset CreatedAtUtc);
