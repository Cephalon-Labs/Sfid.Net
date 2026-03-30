using Sfid.Net.Abstractions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sfid.Net.Serialization;

/// <summary>
/// Serializes strongly typed <see cref="ISfid{TSelf}"/> values as JSON strings and accepts either strings or integers on input.
/// </summary>
/// <typeparam name="TId">The strongly typed identifier type.</typeparam>
public sealed class SfidJsonConverter<TId> : JsonConverter<TId>
    where TId : struct, ISfid<TId>
{
    /// <inheritdoc />
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => SfidJsonConverter.ReadCore<TId>(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
}
