namespace AnonyMeow.Common.Exceptions;

public class FriendshipAlreadyExistsException()
    : ApiException(
        StatusCodes.Status409Conflict,
        "Friendship Already Exists",
        "A friendship or pending friend request already exists between these users.");
