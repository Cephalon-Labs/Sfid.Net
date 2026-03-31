using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SfidNet.EntityFramework;

/// <summary>
/// Applies Sfid conventions automatically during EF Core model customization.
/// </summary>
public sealed class SfidModelCustomizer : ModelCustomizer
{
    /// <summary>
    /// Creates a new model customizer.
    /// </summary>
    public SfidModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        modelBuilder.ApplySfidConventions();
    }
}
