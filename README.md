<p align="center">
  <img src="assets/logo/sfid-logo-source.svg" alt="Sfid.Net logo" width="360" />
</p>

# Sfid.Net

`Sfid.Net` is a .NET library for generating Twitter Snowflake-compatible 64-bit distributed identifiers. It supports raw `long` IDs, strongly typed IDs, process-wide runtime helpers, host-based bootstrapping, and optional Entity Framework Core integration.

## Packages

- `Sfid.Net`: core generator, parser, runtime, typed ID abstractions, and dependency injection helpers.
- `Sfid.EntityFramework`: EF Core converters, comparers, key configuration helpers, and generate-on-save support.

## Installation

```bash
dotnet add package Sfid.Net
dotnet add package Sfid.EntityFramework
```

Install `Sfid.EntityFramework` only when you need EF Core mapping helpers.

## Quick Start

```csharp
using Sfid.Net;

var generator = new SfidGenerator(
    new SfidOptions
    {
        DatacenterId = 1,
        WorkerId = 7,
        ClockRegressionTolerance = TimeSpan.FromMilliseconds(2),
    });

long rawId = generator.NextId();
Sfid typedSfid = generator.Next<Sfid>();
```

You can also define strongly typed identifiers:

```csharp
using Sfid.Net.Abstractions;

public readonly record struct OrderId(long Value) : ISfid<OrderId>
{
    public static OrderId FromInt64(long value) => new(value);
}

OrderId orderId = generator.Next<OrderId>();
```

## Dependency Injection

The library can bootstrap a process-wide runtime generator from configuration and host metadata:

```csharp
builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
```

You can also configure it directly in code:

```csharp
builder.Services.AddSnowfake(options =>
{
    options.DatacenterId = 1;
    options.WorkerId = 7;
    options.WorkerCapacity = 32;
});
```

The configuration section name is currently `Snowfake`, and the host bootstrapper can derive node identity from application and instance metadata when explicit IDs are not supplied.

For a full configuration matrix with `appsettings.json`, environment variables, fallback rules, and production deployment patterns, see the [appsettings configuration guide](https://github.com/Cephalon-Labs/Sfid.Net/blob/master/docs/appsettings-configuration.md).

## Parsing and Runtime Helpers

```csharp
var parsed = SfidParser.Parse<OrderId>("123456789012345678");
var fromLong = SfidParser.FromInt64<Sfid>(123456789012345678);

SfidRuntime.Bootstrap(new SfidOptions
{
    DatacenterId = 2,
    WorkerId = 9,
});

Sfid generated = Sfid.Generate();
```

## JSON for APIs

`Sfid` works out of the box in JSON request and response bodies because the type carries its own `System.Text.Json` converter. If you also expose custom strongly typed IDs that implement `ISfid<TSelf>`, register the converter factory for both Minimal APIs and controllers:

```csharp
using Microsoft.AspNetCore.Http.Json;
using Sfid.Net.Serialization;

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new SfidJsonConverterFactory());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new SfidJsonConverterFactory());
    });
```

The converters write IDs as JSON strings and accept either strings or integers on input. `Sfid` also implements `IParsable<Sfid>` and exposes `Parse` and `TryParse`, which makes route and query binding straightforward in Minimal APIs and MVC. Custom strongly typed IDs should add matching static parse methods if you want direct route and query binding for those types as well.

## Entity Framework Core

```csharp
using Microsoft.EntityFrameworkCore;
using Sfid.EntityFramework;
using Sfid.Net;

public sealed class OrderEntity
{
    public Sfid Id { get; set; }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<OrderEntity>(entity =>
    {
        entity.HasKey(order => order.Id);
        entity.Property(order => order.Id).HasSnowfakeKey();
    });
}
```

For providers that prefer strings:

```csharp
entity.Property(order => order.Id).HasSnowfakeKey(SfidStorageKind.String);
```

## Documentation

- Reference guide: https://github.com/Cephalon-Labs/Sfid.Net/blob/master/snowfake.md
- Appsettings configuration guide: https://github.com/Cephalon-Labs/Sfid.Net/blob/master/docs/appsettings-configuration.md
- Benchmarking guide: https://github.com/Cephalon-Labs/Sfid.Net/blob/master/docs/benchmarking.md
- Validation snapshot: https://github.com/Cephalon-Labs/Sfid.Net/blob/master/docs/verification.md
- NuGet publishing guide: https://github.com/Cephalon-Labs/Sfid.Net/blob/master/docs/publishing-to-nuget.md

## Development

```bash
dotnet test Sfid.Net.slnx
dotnet run --project tests/Sfid.Benchmark/Sfid.Benchmark.csproj -c Release -- --filter *
pwsh ./eng/Pack.ps1 -Configuration Release
```
