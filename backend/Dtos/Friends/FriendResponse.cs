namespace AnonyMeow.Dtos.Friends;

public record FriendResponse(string Username, string? DisplayName, string? AvatarSeed, DateTimeOffset FriendsSinceUtc);
