namespace AnonyMeow.Common.Exceptions;

public class InvalidReplyTargetException()
    : ApiException(
        StatusCodes.Status422UnprocessableEntity,
        "Invalid Reply Target",
        "The message you're replying to doesn't exist in this conversation.");
