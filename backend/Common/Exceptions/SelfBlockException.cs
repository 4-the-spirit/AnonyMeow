namespace AnonyMeow.Common.Exceptions;

public class SelfBlockException()
    : ApiException(StatusCodes.Status403Forbidden, "Self-Block Not Allowed", "You cannot block yourself.");
