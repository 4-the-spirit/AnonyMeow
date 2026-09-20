namespace AnonyMeow.Common.Exceptions;

public class PiiDetectedException(IReadOnlyList<string> detectedCategories)
    : ApiException(
        StatusCodes.Status422UnprocessableEntity,
        "Content Blocked",
        "Your submission appears to contain personal information and was not published.",
        new Dictionary<string, object?> { ["detectedCategories"] = detectedCategories });
