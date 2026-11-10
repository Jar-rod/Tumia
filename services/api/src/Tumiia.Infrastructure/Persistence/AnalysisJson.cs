using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tumiia.Infrastructure.Persistence;

/// <summary>How analysis and taxonomy values are written as JSON in the database: camelCase, enums as names.</summary>
public static class AnalysisJson
{
    /// <summary>The shared serializer options.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Writes a value as JSON.</summary>
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Reads a value back from JSON.</summary>
    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidOperationException($"The stored JSON is not a {typeof(T).Name}.");
}
