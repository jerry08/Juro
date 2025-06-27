using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Juro.Core.Converters;

internal class IntegerConverter : JsonConverter<int>
{
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);

    public override int Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        reader.TokenType switch
        {
            JsonTokenType.String => int.TryParse(reader.GetString(), out var b)
                ? b
                : throw new JsonException(),
            JsonTokenType.Number => reader.TryGetInt32(out var l) ? l : throw new JsonException(),
            _ => throw new JsonException(),
        };
}
