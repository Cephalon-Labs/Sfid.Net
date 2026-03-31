using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using SfidNet.Abstractions;

namespace SfidNet.EntityFramework;

/// <summary>
/// EF Core value generator for typed Snowfake primary keys.
/// </summary>
public sealed class SfidValueGenerator<TId> : ValueGenerator<TId>
    where TId : struct, ISfid<TId>
{
    private readonly ISfidGenerator _generator;

    /// <summary>
    /// Creates a new value generator.
    /// </summary>
    public SfidValueGenerator(ISfidGenerator generator)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;

    /// <inheritdoc />
    public override TId Next(EntityEntry entry)
        => _generator.Next<TId>();
}
