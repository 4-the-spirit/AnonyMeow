namespace AnonyMeow.Common.Exceptions;

public class FlairNameConflictException(string name)
    : ApiException(StatusCodes.Status409Conflict, "Flair Name Unavailable", $"A flair named '{name}' already exists in this community.");
