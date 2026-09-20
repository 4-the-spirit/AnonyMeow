namespace AnonyMeow.Common.Exceptions;

public class PlatformRestrictedException()
    : ApiException(
        StatusCodes.Status403Forbidden, "Account Restricted",
        "Your account is currently restricted by a platform administrator and cannot post or comment.");
