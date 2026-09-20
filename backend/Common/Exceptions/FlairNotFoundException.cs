namespace AnonyMeow.Common.Exceptions;

public class FlairNotFoundException()
    : ApiException(StatusCodes.Status404NotFound, "Flair Not Found", "The specified flair does not exist.");
