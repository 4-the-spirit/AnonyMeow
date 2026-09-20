namespace AnonyMeow.Common.Exceptions;

public class NativeAuthInvalidCodeException()
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Invalid Verification Code",
        "That code is incorrect or has expired.",
        new Dictionary<string, object?>
        {
            ["errors"] = new Dictionary<string, string[]> { ["code"] = ["That code is incorrect or has expired."] }
        });
