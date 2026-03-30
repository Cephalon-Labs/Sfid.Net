using Sfid.Net.Abstractions;
using System.Globalization;

namespace Sfid.Net;

/// <summary>
/// Default strongly typed Snowfake identifier.
/// </summary>
/// <param name="Value">The raw Snowfake value.</param>
public readonly record struct Sfid(long Value) : ISfid<Sfid>
{
    /// <summary>
    /// Creates a typed identifier from a raw 64-bit value.
    /// </summary>
    public static Sfid FromInt64(long value)
        => new(value);

    /// <summary>
    /// Generates a new identifier from the current Snowfake runtime generator.
    /// </summary>
    public static Sfid Generate()
        => SfidRuntime.Next<Sfid>();

    /// <inheritdoc />
    public override string ToString()
        => Value.ToString(CultureInfo.InvariantCulture);
}
