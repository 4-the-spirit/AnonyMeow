namespace AnonyMeow.Common.Exceptions;

public class PostImageTooLargeException(string blobUrl, long maxBytes)
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Image Too Large",
        $"One of the submitted images exceeds the maximum allowed size of {maxBytes} bytes, or could not be found.",
        new Dictionary<string, object?> { ["blobUrl"] = blobUrl });
