namespace AnonyMeow.Common.Exceptions;

public class CommunityNameConflictException(string name)
    : ApiException(StatusCodes.Status409Conflict, "Community Name Unavailable", $"A community named '{name}' already exists.");
