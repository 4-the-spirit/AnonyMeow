namespace AnonyMeow.Common.Exceptions;

public class UnsupportedImageContentTypeException(string blobUrl)
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Unsupported Image Type",
        "One of the submitted images is not a recognized image format (JPEG, PNG, WEBP, or GIF).",
        new Dictionary<string, object?> { ["blobUrl"] = blobUrl });
