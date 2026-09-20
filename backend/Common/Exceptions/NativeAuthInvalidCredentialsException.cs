namespace AnonyMeow.Common.Exceptions;

public class NativeAuthInvalidCredentialsException()
    : ApiException(
        StatusCodes.Status401Unauthorized,
        "Invalid Credentials",
        "Incorrect password.");
