namespace SfidNet.Abstractions;

/// <summary>
/// Produces cluster-safe Snowfake identifiers.
/// </summary>
public interface ISfidGenerator
{
    /// <summary>
    /// Generates the next raw 64-bit identifier.
    /// </summary>
    /// <returns>A unique Snowfake value.</returns>
    long NextId();

    /// <summary>
    /// Generates the next strongly typed identifier.
    /// </summary>
    /// <typeparam name="TId">The strongly typed identifier.</typeparam>
    /// <returns>A unique typed identifier.</returns>
    TId Next<TId>()
        where TId : struct, ISfid<TId>;

    /// <summary>
    /// Breaks a raw identifier into timestamp, datacenter, worker, and sequence parts.
    /// </summary>
    /// <param name="value">The raw Snowfake value.</param>
    /// <returns>The decomposed identifier parts.</returns>
    SfidParts Decompose(long value);
}
