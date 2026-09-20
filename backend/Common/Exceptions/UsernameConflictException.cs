namespace AnonyMeow.Common.Exceptions;

public class UsernameConflictException(string username)
    : ApiException(StatusCodes.Status409Conflict, "Username Unavailable", $"The username '{username}' is already taken.");
