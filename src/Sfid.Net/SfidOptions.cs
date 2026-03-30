namespace Sfid.Net;

/// <summary>
/// Configures the Snowfake generator layout and cluster node identity.
/// </summary>
public sealed class SfidOptions
{
    /// <summary>
    /// Gets or sets the custom epoch used for generated identifiers.
    /// </summary>
    public DateTimeOffset CustomEpoch { get; set; } = SfidDefaults.TwitterEpoch;

    /// <summary>
    /// Gets or sets the datacenter identifier for the current node.
    /// </summary>
    public int DatacenterId { get; set; }

    /// <summary>
    /// Gets or sets the worker identifier within the datacenter.
    /// </summary>
    public int WorkerId { get; set; }

    /// <summary>
    /// Gets or sets the configured worker slot capacity for the current identifier layout.
    /// </summary>
    public int WorkerCapacity { get; set; } = SfidDefaults.DefaultWorkerCapacity;

    /// <summary>
    /// Gets or sets the tolerated clock regression before generation fails.
    /// </summary>
    public TimeSpan ClockRegressionTolerance { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Validates the configured options.
    /// </summary>
    public void Validate()
    {
        if (WorkerCapacity < 1 || WorkerCapacity > SfidDefaults.MaxExpandedWorkerCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(WorkerCapacity),
                WorkerCapacity,
                $"WorkerCapacity must be between 1 and {SfidDefaults.MaxExpandedWorkerCapacity}.");
        }

        if (DatacenterId < 0 || DatacenterId > SfidDefaults.MaxDatacenterId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DatacenterId),
                DatacenterId,
                $"DatacenterId must be between 0 and {SfidDefaults.MaxDatacenterId}.");
        }

        if (WorkerId < 0 || WorkerId >= WorkerCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(WorkerId),
                WorkerId,
                $"WorkerId must be between 0 and {WorkerCapacity - 1}.");
        }

        if (CustomEpoch > DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(CustomEpoch), CustomEpoch, "CustomEpoch must not be in the future.");
        }

        if (ClockRegressionTolerance < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ClockRegressionTolerance),
                ClockRegressionTolerance,
                "ClockRegressionTolerance must be zero or positive.");
        }
    }
}
