namespace AnonyMeow.Common.Exceptions;

public class InvalidParentCommentException()
    : ApiException(
        StatusCodes.Status422UnprocessableEntity, "Invalid Parent Comment", "The parent comment does not belong to the same post.");
