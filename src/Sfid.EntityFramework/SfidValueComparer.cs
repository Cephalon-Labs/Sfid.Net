using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sfid.Net.Abstractions;

namespace Sfid.EntityFramework;

/// <summary>
/// Provides stable equality semantics for typed Snowfake identifiers in EF Core change tracking.
/// </summary>
public sealed class SfidValueComparer<TId> : ValueComparer<TId>
    where TId : struct, ISfid<TId>
{
    /// <summary>
    /// Creates a new comparer instance.
    /// </summary>
    public SfidValueComparer()
        : base(
            (left, right) => left.Value == right.Value,
            identifier => identifier.Value.GetHashCode(),
            identifier => Clone(identifier))
    {
    }

    private static TId Clone(TId identifier)
        => TId.FromInt64(identifier.Value);
}
