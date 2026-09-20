namespace AnonyMeow.Common.Exceptions;

public class SelfFriendRequestException()
    : ApiException(StatusCodes.Status400BadRequest, "Self-Friend-Request Not Allowed", "You cannot send a friend request to yourself.");
