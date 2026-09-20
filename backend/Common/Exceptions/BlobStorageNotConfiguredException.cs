namespace AnonyMeow.Common.Exceptions;

public class BlobStorageNotConfiguredException()
    : ApiException(
        StatusCodes.Status503ServiceUnavailable,
        "Image Upload Unavailable",
        "Azure Blob Storage is not configured yet; image uploads are unavailable until it is provisioned.");
