using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SfidNet.Serialization;

/// <summary>
/// Serializes <see cref="T:SfidNet.Sfid"/> values as JSON strings and accepts either strings or integers on input.
/// </summary>
public sealed class SfidJsonConverter : JsonConverter<Sfid>
{
    /// <inheritdoc />
    public override Sfid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadCore(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Sfid value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));

    internal static TId ReadCore<TId>(ref Utf8JsonReader reader)
        where TId : struct, Abstractions.ISfid<TId>
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ParseString<TId>(reader.GetString()),
            JsonTokenType.Number when reader.TryGetInt64(out var value) => TId.FromInt64(value),
            JsonTokenType.Null => throw new JsonException("Sfid values cannot be null."),
            _ => throw new JsonException("Expected a JSON string or integer for an Sfid value.")
        };
    }

    private static Sfid ReadCore(ref Utf8JsonReader reader)
        => ReadCore<Sfid>(ref reader);

    private static TId ParseString<TId>(string? value)
        where TId : struct, Abstractions.ISfid<TId>
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("Sfid string values must not be empty.");

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            throw new JsonException($"'{value}' is not a valid Sfid value.");

        return TId.FromInt64(parsed);
    }
}
