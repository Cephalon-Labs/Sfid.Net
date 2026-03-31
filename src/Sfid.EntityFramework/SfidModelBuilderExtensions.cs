using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SfidNet.Abstractions;

namespace SfidNet.EntityFramework;

/// <summary>
/// EF Core model conventions for typed Sfid identifiers.
/// </summary>
public static class SfidModelBuilderExtensions
{
    /// <summary>
    /// Applies Snowfake conversion and key-generation conventions to every typed Sfid property in the model.
    /// </summary>
    public static ModelBuilder ApplySfidConventions(
        this ModelBuilder modelBuilder,
        SfidStorageKind storageKind = SfidStorageKind.Int64,
        SfidStorageKind keyStorageKind = SfidStorageKind.Int64)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(property => ImplementsTypedSfidInterface(property.ClrType)))
            {
                // Respect explicit per-property configuration when a converter was already assigned.
                if (property.GetValueConverter() is not null)
                    continue;

                ConfigureProperty(property, property.FindContainingPrimaryKey() is not null ? keyStorageKind : storageKind);

                if (property.FindContainingPrimaryKey() is not null)
                {
                    property.SetAnnotation(SfidPropertyAnnotations.GenerateOnSave, true);
                    property.ValueGenerated = ValueGenerated.Never;
                }
            }
        }

        return modelBuilder;
    }

    private static void ConfigureProperty(IMutableProperty property, SfidStorageKind storageKind)
    {
        property.SetValueComparer(CreateValueComparer(property.ClrType));

        switch (storageKind)
        {
            case SfidStorageKind.String:
                property.SetValueConverter(CreateValueConverter(typeof(SfidToStringConverter<>), property.ClrType));
                property.SetMaxLength(32);
                break;
            default:
                property.SetValueConverter(CreateValueConverter(typeof(SfidToInt64Converter<>), property.ClrType));
                break;
        }
    }

    private static ValueConverter CreateValueConverter(Type openGenericConverterType, Type identifierType)
        => (ValueConverter)Activator.CreateInstance(openGenericConverterType.MakeGenericType(identifierType))!;

    private static ValueComparer CreateValueComparer(Type identifierType)
        => (ValueComparer)Activator.CreateInstance(typeof(SfidValueComparer<>).MakeGenericType(identifierType))!;

    private static bool ImplementsTypedSfidInterface(Type identifierType)
        => identifierType
            .GetInterfaces()
            .Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ISfid<>) &&
                interfaceType.GenericTypeArguments[0] == identifierType);
}
