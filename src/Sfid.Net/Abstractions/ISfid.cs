namespace Sfid.Net.Abstractions;

/// <summary>
/// Represents a strongly typed Snowfake-backed identifier.
/// </summary>
/// <typeparam name="TSelf">The identifier type itself.</typeparam>
public interface ISfid<TSelf>
    where TSelf : struct, ISfid<TSelf>
{
    /// <summary>
    /// Gets the raw 64-bit Snowfake value.
    /// </summary>
    long Value { get; }

    /// <summary>
    /// Creates an identifier instance from a raw 64-bit value.
    /// </summary>
    /// <param name="value">The raw Snowfake value.</param>
    /// <returns>A typed identifier.</returns>
    static abstract TSelf FromInt64(long value);
}
