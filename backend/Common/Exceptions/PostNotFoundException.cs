namespace AnonyMeow.Common.Exceptions;

public class PostNotFoundException()
    : ApiException(StatusCodes.Status404NotFound, "Post Not Found", "The given post does not exist.");
