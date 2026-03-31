using SfidNet.Abstractions;
using SfidNet.Serialization;
using System.Globalization;
using System.Text.Json.Serialization;

namespace SfidNet;

/// <summary>
/// Default strongly typed Snowfake identifier.
/// </summary>
/// <param name="Value">The raw Snowfake value.</param>
[JsonConverter(typeof(SfidJsonConverter))]
public readonly record struct Sfid(long Value) : ISfid<Sfid>, IParsable<Sfid>
{
    /// <summary>
    /// Creates a typed identifier from a raw 64-bit value.
    /// </summary>
    public static Sfid FromInt64(long value)
        => new(value);

    /// <summary>
    /// Parses an identifier from its string representation.
    /// </summary>
    public static Sfid Parse(string value, IFormatProvider? provider)
        => new(long.Parse(value, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture));

    /// <summary>
    /// Parses an identifier from its string representation.
    /// </summary>
    public static Sfid Parse(string value)
        => Parse(value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Attempts to parse an identifier from its string representation.
    /// </summary>
    public static bool TryParse(string? value, IFormatProvider? provider, out Sfid result)
    {
        if (long.TryParse(value, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out var parsed))
        {
            result = new Sfid(parsed);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse an identifier from its string representation.
    /// </summary>
    public static bool TryParse(string? value, out Sfid result)
        => TryParse(value, CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Generates a new identifier from the current Snowfake runtime generator.
    /// </summary>
    public static Sfid Generate()
        => SfidRuntime.Next<Sfid>();

    /// <inheritdoc />
    public override string ToString()
        => Value.ToString(CultureInfo.InvariantCulture);
}
