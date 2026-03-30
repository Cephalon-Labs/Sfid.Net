using Microsoft.EntityFrameworkCore;
using Sfid.Net;
using Sfid.Net.Abstractions;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Sfid.EntityFramework;

/// <summary>
/// DbContext helpers for Snowfake-backed identifiers.
/// </summary>
public static class SfidDbContextExtensions
{
    private static readonly ConcurrentDictionary<Type, Func<ISfidGenerator, object>> s_valueFactoryCache = new();

    /// <summary>
    /// Assigns Snowfake identifiers to added entities whose Snowfake key properties are still unset.
    /// </summary>
    public static void AssignSnowfakeKeys(this DbContext dbContext, ISfidGenerator? generator = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        generator ??= SfidRuntime.Current;
        var assignedAnyKeys = false;

        foreach (var entry in dbContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
        {
            foreach (var property in entry.Properties.Where(x => IsGenerateOnSaveProperty(x.Metadata)))
            {
                if (!IsDefaultValue(property.CurrentValue, property.Metadata.ClrType))
                    continue;

                property.CurrentValue = CreateIdentifier(generator, property.Metadata.ClrType);
                assignedAnyKeys = true;
            }
        }

        if (assignedAnyKeys)
            dbContext.ChangeTracker.DetectChanges();
    }

    private static bool IsGenerateOnSaveProperty(Microsoft.EntityFrameworkCore.Metadata.IProperty property)
    {
        var explicitSetting = property.FindAnnotation(SfidPropertyAnnotations.GenerateOnSave)?.Value as bool?;
        if (explicitSetting.HasValue)
            return explicitSetting.Value;

        return property.FindContainingPrimaryKey() is not null &&
               ImplementsTypedSfidInterface(property.ClrType);
    }

    private static bool IsDefaultValue(object? value, Type propertyType)
    {
        if (value is null)
            return true;

        var defaultValue = propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        return Equals(value, defaultValue);
    }

    private static object CreateIdentifier(ISfidGenerator generator, Type identifierType)
        => s_valueFactoryCache.GetOrAdd(identifierType, BuildValueFactory)(generator);

    private static Func<ISfidGenerator, object> BuildValueFactory(Type identifierType)
    {
        if (!ImplementsTypedSfidInterface(identifierType))
        {
            throw new InvalidOperationException(
                $"Type '{identifierType.FullName}' must implement ISfid<{identifierType.Name}> to use HasSnowfakeKey().");
        }

        var generatorParameter = Expression.Parameter(typeof(ISfidGenerator), "generator");
        var nextMethod = typeof(ISfidGenerator)
            .GetMethod(nameof(ISfidGenerator.Next))!
            .MakeGenericMethod(identifierType);

        var nextCall = Expression.Call(generatorParameter, nextMethod);
        var box = Expression.Convert(nextCall, typeof(object));
        return Expression.Lambda<Func<ISfidGenerator, object>>(box, generatorParameter).Compile();
    }

    private static bool ImplementsTypedSfidInterface(Type identifierType)
        => identifierType
            .GetInterfaces()
            .Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ISfid<>) &&
                interfaceType.GenericTypeArguments[0] == identifierType);
}
