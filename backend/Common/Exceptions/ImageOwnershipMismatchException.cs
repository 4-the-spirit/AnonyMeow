namespace AnonyMeow.Common.Exceptions;

public class ImageOwnershipMismatchException(string blobUrl)
    : ApiException(
        StatusCodes.Status403Forbidden,
        "Image Not Owned",
        "One of the submitted images does not belong to the current user.",
        new Dictionary<string, object?> { ["blobUrl"] = blobUrl });
