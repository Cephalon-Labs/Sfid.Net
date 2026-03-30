namespace Sfid.Net;

/// <summary>
/// Configuration settings used when bootstrapping Snowfake from an application host.
/// </summary>
public sealed class SnowfakeSettings
{
    /// <summary>
    /// The configuration section name used by Snowfake.
    /// </summary>
    public const string SectionName = "Snowfake";

    /// <summary>
    /// Gets or sets the datacenter identifier for the current node.
    /// </summary>
    public int? DatacenterId { get; set; }

    /// <summary>
    /// Gets or sets the worker identifier for the current node.
    /// </summary>
    public int? WorkerId { get; set; }

    /// <summary>
    /// Gets or sets the total worker slot capacity for the current identifier layout.
    /// </summary>
    public int? WorkerCapacity { get; set; }

    /// <summary>
    /// Gets or sets the custom epoch used for generated identifiers.
    /// </summary>
    public DateTimeOffset? CustomEpoch { get; set; }

    /// <summary>
    /// Gets or sets the tolerated clock regression before generation fails.
    /// </summary>
    public int ClockRegressionToleranceMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets an optional explicit instance identifier for the current process.
    /// </summary>
    public string? InstanceId { get; set; }
}
