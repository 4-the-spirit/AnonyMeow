namespace AnonyMeow.Common.Exceptions;

public class VoteTargetNotFoundException()
    : ApiException(StatusCodes.Status404NotFound, "Vote Target Not Found", "The post or comment being voted on does not exist.");
