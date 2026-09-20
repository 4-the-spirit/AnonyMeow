using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AnonyMeow.Common.Middleware;

public static class ValidationProblemFactory
{
    // 422, not TypedResults.ValidationProblem's fixed 400: these are well-formed requests that
    // fail a semantic rule (format, cross-field, business validation), not malformed JSON/routing.
    // TypedResults.ValidationProblem has no way to override its status code, so this builds the
    // same response shape (HttpValidationProblemDetails) via TypedResults.Json instead.
    public static JsonHttpResult<HttpValidationProblemDetails> Create(IDictionary<string, string[]> errors)
    {
        var details = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "One or more validation errors occurred."
        };
        return TypedResults.Json(details, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    public static JsonHttpResult<HttpValidationProblemDetails> Create(string key, string message) =>
        Create(new Dictionary<string, string[]> { [key] = [message] });
}
