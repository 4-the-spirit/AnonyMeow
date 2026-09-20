namespace AnonyMeow.Common.Exceptions;

public class SelfVoteException()
    : ApiException(StatusCodes.Status403Forbidden, "Self-Vote Not Allowed", "You cannot vote on your own post or comment.");
