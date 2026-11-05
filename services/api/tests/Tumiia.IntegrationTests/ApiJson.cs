using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tumiia.IntegrationTests;

/// <summary>Reads API responses the way the API writes them: camelCase, with enums as names ("New", "drag_drop").</summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
