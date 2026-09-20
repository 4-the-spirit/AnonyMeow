namespace AnonyMeow.Common.Exceptions;

public class FlairCommunityMismatchException()
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Flair Community Mismatch",
        "The flair does not belong to the same community as the post.");
