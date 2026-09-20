namespace AnonyMeow.Dtos.Communities;

public record CommunityModeratorResponse(string Username, string? DisplayName, DateTimeOffset JoinedAtUtc);
