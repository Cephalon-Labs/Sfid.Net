using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SfidNet.EntityFramework;
using SfidNet;

namespace SfidNet.Test;

public sealed class SfidEntityFrameworkTests
{
    private static string NewDatabaseName()
        => Guid.NewGuid().ToString("N");

    [Fact]
    public void Int64Converter_ShouldRoundTripTypedIdentifier()
    {
        var converter = new SfidToInt64Converter<OrderId>();
        var identifier = new OrderId(123456789012345678);

        var converted = converter.ConvertToProviderExpression.Compile()(identifier);
        var restored = converter.ConvertFromProviderExpression.Compile()(converted);

        converted.Should().Be(identifier.Value);
        restored.Should().Be(identifier);
    }

    [Fact]
    public void StringConverter_ShouldRoundTripTypedIdentifier()
    {
        var converter = new SfidToStringConverter<OrderId>();
        var identifier = new OrderId(123456789012345678);

        var converted = converter.ConvertToProviderExpression.Compile()(identifier);
        var restored = converter.ConvertFromProviderExpression.Compile()(converted);

        converted.Should().Be("123456789012345678");
        restored.Should().Be(identifier);
    }

    [Fact]
    public void ValueComparer_ShouldProvideStableEqualityHashingAndSnapshots()
    {
        var comparer = new SfidValueComparer<OrderId>();
        var equals = comparer.EqualsExpression.Compile();
        var hash = comparer.HashCodeExpression.Compile();
        var snapshot = comparer.SnapshotExpression.Compile();
        var left = new OrderId(42);
        var right = new OrderId(42);

        equals(left, right).Should().BeTrue();
        hash(left).Should().Be(hash(right));
        snapshot(left).Should().Be(left);
    }

    [Fact]
    public void ValueGenerator_ShouldGenerateNonTemporaryTypedIdentifiers()
    {
        var options = new DbContextOptionsBuilder<Int64StorageDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;
        using var context = new Int64StorageDbContext(options);
        var generator = new SfidValueGenerator<OrderId>(new StubSfidGenerator(321));

        var value = generator.Next(context.Entry(new OrderEntity()));

        generator.GeneratesTemporaryValues.Should().BeFalse();
        value.Should().Be(new OrderId(321));
    }

    [Fact]
    public void HasSnowfakeConversion_ShouldConfigureInt64StorageByDefault()
    {
        var options = new DbContextOptionsBuilder<Int64StorageDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new Int64StorageDbContext(options);
        var property = context.Model.FindEntityType(typeof(OrderEntity))?.FindProperty(nameof(OrderEntity.Id));

        property.Should().NotBeNull();
        property!.GetValueConverter().Should().BeOfType<SfidToInt64Converter<OrderId>>();
        property.GetValueComparer().Should().BeOfType<SfidValueComparer<OrderId>>();
        property.GetMaxLength().Should().BeNull();
    }

    [Fact]
    public void HasSnowfakeKey_ShouldConfigureStringStorageForUnsupportedProviders()
    {
        var options = new DbContextOptionsBuilder<StringStorageDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new StringStorageDbContext(options);
        var property = context.Model.FindEntityType(typeof(OrderEntity))?.FindProperty(nameof(OrderEntity.Id));

        property.Should().NotBeNull();
        property!.ValueGenerated.Should().Be(ValueGenerated.Never);
        property.GetMaxLength().Should().Be(32);
        property.GetValueConverter().Should().BeOfType<SfidToStringConverter<OrderId>>();
        property.GetValueComparer().Should().BeOfType<SfidValueComparer<OrderId>>();
    }

    [Fact]
    public void ApplySfidConventions_ShouldConfigureTypedPropertiesWithoutPerPropertyCalls()
    {
        var options = new DbContextOptionsBuilder<ConventionConfiguredDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new ConventionConfiguredDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ConventionConfiguredEntity));
        var idProperty = entityType?.FindProperty(nameof(ConventionConfiguredEntity.Id));
        var customerIdProperty = entityType?.FindProperty(nameof(ConventionConfiguredEntity.CustomerId));

        idProperty.Should().NotBeNull();
        idProperty!.ValueGenerated.Should().Be(ValueGenerated.Never);
        idProperty.GetValueConverter().Should().BeOfType<SfidToInt64Converter<OrderId>>();
        idProperty.GetValueComparer().Should().BeOfType<SfidValueComparer<OrderId>>();
        idProperty.FindAnnotation("Snowfake:GenerateOnSave")?.Value.Should().Be(true);

        customerIdProperty.Should().NotBeNull();
        customerIdProperty!.GetValueConverter().Should().BeOfType<SfidToInt64Converter<CustomerId>>();
        customerIdProperty.GetValueComparer().Should().BeOfType<SfidValueComparer<CustomerId>>();
    }

    [Fact]
    public void ApplySfidConventions_ShouldRespectExplicitPerPropertyConfiguration()
    {
        var options = new DbContextOptionsBuilder<ConventionWithExplicitStringKeyDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new ConventionWithExplicitStringKeyDbContext(options);
        var property = context.Model.FindEntityType(typeof(OrderEntity))?.FindProperty(nameof(OrderEntity.Id));

        property.Should().NotBeNull();
        property!.GetValueConverter().Should().BeOfType<SfidToStringConverter<OrderId>>();
        property.GetMaxLength().Should().Be(32);
    }

    [Fact]
    public void UseSfidEntityFramework_ShouldExposeDefaultValueConvertersForTypedIdentifiers()
    {
        var options = new DbContextOptionsBuilder<UnconfiguredAutoMappingDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .UseSfidEntityFramework()
            .Options;

        using var context = new UnconfiguredAutoMappingDbContext(options);
        var selector = context.GetService<IValueConverterSelector>();

        selector.Should().BeOfType<SfidValueConverterSelector>();

        var converters = selector.Select(typeof(OrderId)).ToList();
        converters.Should().Contain(info => info.ProviderClrType == typeof(long));
        converters.Should().Contain(info => info.ProviderClrType == typeof(string));
    }

    [Fact]
    public void AssignSnowfakeKeys_ShouldAutoGeneratePrimaryKeyWithoutHasSnowfakeKey()
    {
        using var _ = new RuntimeScope();
        SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 6,
                WorkerId = 21,
            });

        var options = new DbContextOptionsBuilder<UnconfiguredAutoMappingDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .UseSfidEntityFramework()
            .Options;

        using var context = new UnconfiguredAutoMappingDbContext(options);
        var entity = new UnconfiguredAutoMappingEntity
        {
            CustomerId = new CustomerId(77),
        };

        context.Entities.Add(entity);
        context.SaveChanges();

        entity.Id.Should().NotBe(default(OrderId));
        entity.CustomerId.Should().Be(new CustomerId(77));
    }

    [Fact]
    public void AssignSnowfakeKeys_ShouldGenerateIdentifiersDuringSaveChangesOnly()
    {
        using var _ = new RuntimeScope();
        var fixedTime = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-03-18T02:03:04Z"));
        var runtimeGenerator = (SfidGenerator)SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 8,
                WorkerId = 12,
            },
            fixedTime);

        var options = new DbContextOptionsBuilder<AutoGeneratedKeyDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new AutoGeneratedKeyDbContext(options);
        var entity = new AutoGeneratedOrderEntity();

        context.Orders.Add(entity);
        entity.Id.Should().Be(default(OrderId));

        context.SaveChanges();

        entity.Id.Should().NotBe(default(OrderId));

        var parts = runtimeGenerator.Decompose(entity.Id.Value);
        parts.Timestamp.Should().Be(fixedTime.GetUtcNow());
        parts.DatacenterId.Should().Be(8);
        parts.WorkerId.Should().Be(12);
        parts.Sequence.Should().Be(0);
    }

    [Fact]
    public async Task AssignSnowfakeKeys_ShouldGenerateIdentifiersDuringSaveChangesAsync()
    {
        using var _ = new RuntimeScope();
        SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 8,
                WorkerId = 13,
            });

        var options = new DbContextOptionsBuilder<AutoGeneratedKeyDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new AutoGeneratedKeyDbContext(options);
        var entity = new AutoGeneratedOrderEntity();

        context.Orders.Add(entity);
        await context.SaveChangesAsync();

        entity.Id.Should().NotBe(default(OrderId));
    }

    [Fact]
    public void AssignSnowfakeKeys_ShouldPreserveExplicitIdentifiers()
    {
        using var _ = new RuntimeScope();
        SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 9,
                WorkerId = 3,
            });

        var options = new DbContextOptionsBuilder<AutoGeneratedKeyDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new AutoGeneratedKeyDbContext(options);
        var entity = new AutoGeneratedOrderEntity
        {
            Id = new OrderId(9876543210),
        };

        context.Orders.Add(entity);
        context.SaveChanges();

        entity.Id.Should().Be(new OrderId(9876543210));
    }

    [Fact]
    public void AssignSnowfakeKeys_ShouldUseSuppliedGeneratorInsteadOfRuntime()
    {
        var options = new DbContextOptionsBuilder<AutoGeneratedKeyDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new AutoGeneratedKeyDbContext(options);
        var entity = new AutoGeneratedOrderEntity();

        context.Orders.Add(entity);
        context.AssignSnowfakeKeys(new StubSfidGenerator(42));

        entity.Id.Should().Be(new OrderId(42));
    }

    [Fact]
    public void AssignSnowfakeKeys_ShouldPropagateGeneratedKeysToTrackedForeignKeys()
    {
        using var _ = new RuntimeScope();
        SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 10,
                WorkerId = 14,
            });

        var options = new DbContextOptionsBuilder<AutoGeneratedRelationshipDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new AutoGeneratedRelationshipDbContext(options);
        var order = new AutoGeneratedParentEntity();
        var line = new AutoGeneratedChildEntity
        {
            Order = order,
        };

        context.Add(line);
        context.SaveChanges();

        order.Id.Should().NotBe(default(OrderId));
        line.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void AssignSnowfakeKeys_ShouldThrowWhenAnnotatedPropertyIsNotASfid()
    {
        var options = new DbContextOptionsBuilder<InvalidSnowfakeKeyDbContext>()
            .UseInMemoryDatabase(NewDatabaseName())
            .Options;

        using var context = new InvalidSnowfakeKeyDbContext(options);
        context.Entities.Add(new InvalidSnowfakeEntity());

        var act = () => context.AssignSnowfakeKeys(new StubSfidGenerator(100));

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Type '*Int32*' must implement ISfid<Int32>*");
    }

    private sealed class OrderEntity
    {
        public OrderId Id { get; set; }
    }

    private sealed class AutoGeneratedOrderEntity
    {
        public OrderId Id { get; set; }
    }

    private sealed class ConventionConfiguredEntity
    {
        public OrderId Id { get; set; }
        public CustomerId CustomerId { get; set; }
    }

    private sealed class UnconfiguredAutoMappingEntity
    {
        public OrderId Id { get; set; }
        public CustomerId CustomerId { get; set; }
    }

    private sealed class AutoGeneratedParentEntity
    {
        public OrderId Id { get; set; }
        public List<AutoGeneratedChildEntity> Lines { get; set; } = [];
    }

    private sealed class AutoGeneratedChildEntity
    {
        public int Id { get; set; }
        public OrderId OrderId { get; set; }
        public AutoGeneratedParentEntity? Order { get; set; }
    }

    private sealed class InvalidSnowfakeEntity
    {
        public Guid Id { get; set; }
        public int InvalidSfid { get; set; }
    }

    private sealed class Int64StorageDbContext(DbContextOptions<Int64StorageDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
                entity.Property(order => order.Id).HasSnowfakeConversion();
            });
        }
    }

    private sealed class StringStorageDbContext(DbContextOptions<StringStorageDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
                entity.Property(order => order.Id).HasSnowfakeKey(SfidStorageKind.String);
            });
        }
    }

    private sealed class AutoGeneratedKeyDbContext(DbContextOptions<AutoGeneratedKeyDbContext> options) : DbContext(options)
    {
        public DbSet<AutoGeneratedOrderEntity> Orders => Set<AutoGeneratedOrderEntity>();

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.AssignSnowfakeKeys();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            this.AssignSnowfakeKeys();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AutoGeneratedOrderEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
                entity.Property(order => order.Id).HasSnowfakeKey();
            });
        }
    }

    private sealed class AutoGeneratedRelationshipDbContext(DbContextOptions<AutoGeneratedRelationshipDbContext> options) : DbContext(options)
    {
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.AssignSnowfakeKeys();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AutoGeneratedParentEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
                entity.Property(order => order.Id).HasSnowfakeKey();
                entity.HasMany(order => order.Lines)
                    .WithOne(line => line.Order)
                    .HasForeignKey(line => line.OrderId);
            });

            modelBuilder.Entity<AutoGeneratedChildEntity>(entity =>
            {
                entity.HasKey(line => line.Id);
                entity.Property(line => line.OrderId).HasSnowfakeConversion();
            });
        }
    }

    private sealed class ConventionConfiguredDbContext(DbContextOptions<ConventionConfiguredDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConventionConfiguredEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
            });

            modelBuilder.ApplySfidConventions();
        }
    }

    private sealed class ConventionWithExplicitStringKeyDbContext(DbContextOptions<ConventionWithExplicitStringKeyDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
                entity.Property(order => order.Id).HasSnowfakeKey(SfidStorageKind.String);
            });

            modelBuilder.ApplySfidConventions();
        }
    }

    private sealed class UnconfiguredAutoMappingDbContext(DbContextOptions<UnconfiguredAutoMappingDbContext> options) : DbContext(options)
    {
        public DbSet<UnconfiguredAutoMappingEntity> Entities => Set<UnconfiguredAutoMappingEntity>();

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.AssignSnowfakeKeys();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnconfiguredAutoMappingEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
            });
        }
    }

    private sealed class InvalidSnowfakeKeyDbContext(DbContextOptions<InvalidSnowfakeKeyDbContext> options) : DbContext(options)
    {
        public DbSet<InvalidSnowfakeEntity> Entities => Set<InvalidSnowfakeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvalidSnowfakeEntity>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.InvalidSfid).HasAnnotation("Snowfake:GenerateOnSave", true);
            });
        }
    }
}
