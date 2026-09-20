namespace AnonyMeow.Common.Exceptions;

public abstract class ApiException(
    int statusCode, string title, string detail, IReadOnlyDictionary<string, object?>? extensions = null)
    : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string Detail { get; } = detail;
    public IReadOnlyDictionary<string, object?> Extensions { get; } = extensions ?? new Dictionary<string, object?>();
}
