using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace SfidNet;

internal static class SfidNodeIdentityResolver
{
    private const int TotalNodeSlotCount = (SfidDefaults.MaxDatacenterId + 1) * SfidDefaults.DefaultWorkerCapacity;
    private const int MaxNodeSlot = TotalNodeSlotCount - 1;

    public static SnowfakeNodeIdentity Resolve(
        SfidSettings settings,
        string applicationName,
        string instanceId)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.DatacenterId.HasValue && !settings.WorkerId.HasValue)
            return FromNodeSlot(ResolveNodeSlot(applicationName, instanceId));

        var workerCapacity = ResolveWorkerCapacity(settings);

        return new SnowfakeNodeIdentity(
            ResolveNodeIdentifier(settings.DatacenterId, applicationName, SfidDefaults.MaxDatacenterId),
            ResolveNodeIdentifier(settings.WorkerId, instanceId, workerCapacity - 1),
            workerCapacity);
    }

    private static SnowfakeNodeIdentity FromNodeSlot(int nodeSlot)
    {
        var clampedNodeSlot = Math.Clamp(nodeSlot, 0, MaxNodeSlot);
        return new SnowfakeNodeIdentity(
            clampedNodeSlot / SfidDefaults.DefaultWorkerCapacity,
            clampedNodeSlot % SfidDefaults.DefaultWorkerCapacity,
            SfidDefaults.DefaultWorkerCapacity);
    }

    private static int ResolveNodeSlot(string applicationName, string instanceId)
        => ResolveHashedIdentifier(BuildNodeSource(applicationName, instanceId), MaxNodeSlot);

    private static int ResolveNodeIdentifier(int? configuredValue, string source, int maxValue)
    {
        if (configuredValue.HasValue)
            return Math.Clamp(configuredValue.Value, 0, maxValue);

        return ResolveHashedIdentifier(source, maxValue);
    }

    private static int ResolveWorkerCapacity(SfidSettings settings)
    {
        var defaultCapacity = settings.DatacenterId.HasValue && !settings.WorkerId.HasValue
            ? SfidDefaults.DefaultFixedDatacenterAutoWorkerCapacity
            : SfidDefaults.DefaultWorkerCapacity;

        return Math.Clamp(settings.WorkerCapacity ?? defaultCapacity, 1, SfidDefaults.MaxExpandedWorkerCapacity);
    }

    private static int ResolveHashedIdentifier(string source, int maxValue)
    {
        if (string.IsNullOrWhiteSpace(source))
            return 0;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        var rawValue = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        return (int)(rawValue % (uint)(maxValue + 1));
    }

    private static string BuildNodeSource(string applicationName, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            return instanceId;

        if (string.IsNullOrWhiteSpace(instanceId))
            return applicationName;

        return $"{applicationName}:{instanceId}";
    }
}

internal readonly record struct SnowfakeNodeIdentity(int DatacenterId, int WorkerId, int WorkerCapacity);
