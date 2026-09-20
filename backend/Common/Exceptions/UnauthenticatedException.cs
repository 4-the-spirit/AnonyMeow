namespace AnonyMeow.Common.Exceptions;

public class UnauthenticatedException()
    : ApiException(StatusCodes.Status401Unauthorized, "Unauthenticated", "No authenticated user could be resolved for this request.");
