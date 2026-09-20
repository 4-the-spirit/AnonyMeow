namespace AnonyMeow.Common.Exceptions;

public class UserBlockedException()
    : ApiException(
        StatusCodes.Status403Forbidden, "Blocked", "This action isn't available because one of you has blocked the other.");
