namespace AnonyMeow.Dtos.Friends;

public record FriendRequestsResponse(
    IReadOnlyList<FriendRequestResponse> Incoming,
    IReadOnlyList<FriendRequestResponse> Outgoing);
