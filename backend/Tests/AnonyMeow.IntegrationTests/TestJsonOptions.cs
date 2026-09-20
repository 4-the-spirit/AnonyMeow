using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnonyMeow.IntegrationTests;

// Mirrors the JsonStringEnumConverter registered via ConfigureHttpJsonOptions in Program.cs,
// so response bodies (which serialize enums as strings) deserialize correctly in tests.
public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
