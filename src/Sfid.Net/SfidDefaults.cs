namespace Sfid.Net;

/// <summary>
/// Shared constants for the Snowfake layout.
/// </summary>
public static class SfidDefaults
{
    /// <summary>
    /// The Twitter Snowflake epoch (2010-11-04T01:42:54.657Z).
    /// </summary>
    public static readonly DateTimeOffset TwitterEpoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657);

    /// <summary>
    /// Gets the number of datacenter bits.
    /// </summary>
    public const int DatacenterBits = 5;

    /// <summary>
    /// Gets the base number of worker bits in the default layout.
    /// </summary>
    public const int WorkerBits = 5;

    /// <summary>
    /// Gets the base number of sequence bits in the default layout.
    /// </summary>
    public const int SequenceBits = 12;

    /// <summary>
    /// Gets the number of timestamp bits.
    /// </summary>
    public const int TimestampBits = 41;

    /// <summary>
    /// Gets the highest valid datacenter identifier.
    /// </summary>
    public const int MaxDatacenterId = (1 << DatacenterBits) - 1;

    /// <summary>
    /// Gets the highest valid worker identifier.
    /// </summary>
    public const int MaxWorkerId = (1 << WorkerBits) - 1;

    /// <summary>
    /// Gets the highest valid worker identifier when the full sequence range is reused for worker expansion.
    /// </summary>
    public const int MaxExpandedWorkerId = (1 << (WorkerBits + SequenceBits)) - 1;

    /// <summary>
    /// Gets the default worker capacity in the standard layout.
    /// </summary>
    public const int DefaultWorkerCapacity = MaxWorkerId + 1;

    /// <summary>
    /// Gets the default worker capacity used when a datacenter is fixed and the worker is auto-assigned.
    /// </summary>
    public const int DefaultFixedDatacenterAutoWorkerCapacity = 1024;

    /// <summary>
    /// Gets the highest supported worker capacity when the full sequence range is reused for worker expansion.
    /// </summary>
    public const int MaxExpandedWorkerCapacity = MaxExpandedWorkerId + 1;

    /// <summary>
    /// Gets the highest sequence value per millisecond.
    /// </summary>
    public const int MaxSequence = (1 << SequenceBits) - 1;
}
