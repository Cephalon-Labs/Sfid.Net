namespace Sfid.Net;

/// <summary>
/// Represents the decomposed parts of a Snowfake identifier.
/// </summary>
/// <param name="Value">The full raw identifier.</param>
/// <param name="Timestamp">The UTC timestamp encoded in the identifier.</param>
/// <param name="DatacenterId">The datacenter identifier.</param>
/// <param name="WorkerId">The worker identifier. This may exceed 31 when expanded worker capacity is enabled.</param>
/// <param name="Sequence">The per-millisecond sequence number. This may use fewer than 12 bits when worker capacity is expanded.</param>
public readonly record struct SfidParts(
    long Value,
    DateTimeOffset Timestamp,
    int DatacenterId,
    int WorkerId,
    int Sequence);
