namespace AnonyMeow.Common.Exceptions;

public class NativeAuthWeakPasswordException(string reason)
    : ApiException(
        StatusCodes.Status422UnprocessableEntity,
        "Password Rejected",
        reason,
        new Dictionary<string, object?>
        {
            ["errors"] = new Dictionary<string, string[]> { ["password"] = [reason] }
        });
