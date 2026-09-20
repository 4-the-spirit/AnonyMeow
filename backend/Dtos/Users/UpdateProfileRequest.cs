using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Users;

public record UpdateProfileRequest(string? DisplayName, string? AvatarSeed, FriendListVisibility? FriendListVisibility);
