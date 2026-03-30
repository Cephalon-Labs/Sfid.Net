using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sfid.EntityFramework;

/// <summary>
/// EF Core options extensions for Sfid integration.
/// </summary>
public static class SfidDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables automatic Sfid value-converter discovery for EF Core model properties.
    /// </summary>
    public static DbContextOptionsBuilder UseSfidEntityFramework(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        return optionsBuilder
            .ReplaceService<IValueConverterSelector, SfidValueConverterSelector>()
            .ReplaceService<IModelCustomizer, SfidModelCustomizer>();
    }

    /// <summary>
    /// Enables automatic Sfid value-converter discovery for EF Core model properties.
    /// </summary>
    public static DbContextOptionsBuilder<TContext> UseSfidEntityFramework<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        ((DbContextOptionsBuilder)optionsBuilder).UseSfidEntityFramework();
        return optionsBuilder;
    }
}
