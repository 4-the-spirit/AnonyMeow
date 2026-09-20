using System.Text.Json;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Common.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnonyMeow.UnitTests.Common;

public class GlobalExceptionHandlerTests
{
    private class FakeHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
    }

    [Fact]
    public async Task TryHandleAsync_PiiDetectedException_ProducesUnprocessableEntity_WithDetectedCategories()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, new FakeHostEnvironment());
        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var handled = await handler.TryHandleAsync(httpContext, new PiiDetectedException(["Email"]), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);

        responseStream.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(responseStream);
        var categories = Assert.IsType<JsonElement>(problemDetails!.Extensions["detectedCategories"]);
        Assert.Equal("Email", categories[0].GetString());
    }
}
