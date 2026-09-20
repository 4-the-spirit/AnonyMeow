namespace AnonyMeow.Common.Exceptions;

public class PostLockedException()
    : ApiException(StatusCodes.Status403Forbidden, "Post Locked", "This post is locked and no longer accepts new comments.");
