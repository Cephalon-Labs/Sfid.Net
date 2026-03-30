using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sfid.Net.Abstractions;

namespace Sfid.EntityFramework;

/// <summary>
/// EF Core model-building helpers for typed Snowfake identifiers.
/// </summary>
public static class SfidPropertyBuilderExtensions
{
    /// <summary>
    /// Configures a typed Snowfake property with the appropriate value converter and comparer.
    /// </summary>
    public static PropertyBuilder<TId> HasSnowfakeConversion<TId>(
        this PropertyBuilder<TId> propertyBuilder,
        SfidStorageKind storageKind = SfidStorageKind.Int64)
        where TId : struct, ISfid<TId>
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        switch (storageKind)
        {
            case SfidStorageKind.String:
                propertyBuilder.HasConversion<SfidToStringConverter<TId>, SfidValueComparer<TId>>();
                propertyBuilder.HasMaxLength(32);
                break;
            default:
                propertyBuilder.HasConversion<SfidToInt64Converter<TId>, SfidValueComparer<TId>>();
                break;
        }

        return propertyBuilder;
    }

    /// <summary>
    /// Configures a typed Snowfake property for use as a primary key or generated identity column.
    /// </summary>
    public static PropertyBuilder<TId> HasSnowfakeKey<TId>(
        this PropertyBuilder<TId> propertyBuilder,
        SfidStorageKind storageKind = SfidStorageKind.Int64)
        where TId : struct, ISfid<TId>
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        return propertyBuilder
            .HasSnowfakeConversion(storageKind)
            .HasAnnotation(SfidPropertyAnnotations.GenerateOnSave, true)
            .ValueGeneratedNever();
    }
}
