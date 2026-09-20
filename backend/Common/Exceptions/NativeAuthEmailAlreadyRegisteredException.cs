namespace AnonyMeow.Common.Exceptions;

public class NativeAuthEmailAlreadyRegisteredException(string email)
    : ApiException(
        StatusCodes.Status409Conflict,
        "Email Already Registered",
        $"An account with the email '{email}' already exists.",
        new Dictionary<string, object?>
        {
            ["errors"] = new Dictionary<string, string[]> { ["email"] = ["An account with this email already exists."] }
        });
