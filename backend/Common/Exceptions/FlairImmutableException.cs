namespace AnonyMeow.Common.Exceptions;

public class FlairImmutableException()
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Default Tag Is Immutable",
        "Default tags can't be edited or deleted.");
