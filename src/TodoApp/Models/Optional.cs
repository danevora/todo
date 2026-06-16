using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoApp.Models;

/// <summary>
/// Distinguishes "field was omitted" from "field was explicitly set to null" in a PATCH body.
/// A property absent from the JSON deserializes to <c>default</c> (<see cref="IsSet"/> = false);
/// a property present in the JSON (even as null) sets <see cref="IsSet"/> = true. This lets a
/// partial update both leave fields untouched and clear nullable fields.
/// </summary>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public bool IsSet { get; }
    public T Value { get; }

    public Optional(T value)
    {
        IsSet = true;
        Value = value;
    }
}

public class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var inner = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(OptionalJsonConverter<>).MakeGenericType(inner))!;
    }
}

public class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize<T>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Value, options);
}
