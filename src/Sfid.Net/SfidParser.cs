using SfidNet.Abstractions;

namespace SfidNet;

/// <summary>
/// Parses raw values into strongly typed Snowfake identifiers.
/// </summary>
public static class SfidParser
{
    /// <summary>
    /// Creates a strongly typed identifier from a raw 64-bit value.
    /// </summary>
    public static TId FromInt64<TId>(long value)
        where TId : struct, ISfid<TId>
        => TId.FromInt64(value);

    /// <summary>
    /// Creates a strongly typed identifier from a string representation.
    /// </summary>
    public static TId Parse<TId>(string value)
        where TId : struct, ISfid<TId>
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", nameof(value));

        return FromInt64<TId>(long.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Attempts to parse a strongly typed identifier from a string representation.
    /// </summary>
    public static bool TryParse<TId>(string? value, out TId identifier)
        where TId : struct, ISfid<TId>
    {
        if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            identifier = FromInt64<TId>(parsed);
            return true;
        }

        identifier = default;
        return false;
    }
}
