namespace AnonyMeow.Common.Exceptions;

public class PollOptionNotFoundException()
    : ApiException(StatusCodes.Status404NotFound, "Poll Option Not Found", "The given poll option does not belong to this post.");
