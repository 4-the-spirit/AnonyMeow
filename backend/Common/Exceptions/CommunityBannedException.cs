namespace AnonyMeow.Common.Exceptions;

public class CommunityBannedException()
    : ApiException(
        StatusCodes.Status403Forbidden, "Banned From Community", "You are banned from this community and cannot post or comment.");
