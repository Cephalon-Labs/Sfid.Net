# Sfid.Net Reference Guide

This file keeps the historic `snowfake.md` name for backwards compatibility, but its content tracks the current `Sfid.*` packages and APIs.

## Overview

`Sfid.Net` implements Twitter Snowflake-compatible 64-bit distributed identifiers for .NET applications. The core goals are:

- sortable numeric IDs without a database round-trip
- support for raw `long` values and strongly typed IDs
- process-wide runtime access for simple applications
- host/bootstrap integration for multi-instance services
- optional EF Core helpers without forcing EF Core into the core package

## Package Layout

- `Sfid.Net`
  Contains `Sfid`, `SfidGenerator`, `SfidRuntime`, `SfidParser`, `SfidOptions`, and the typed ID abstractions.
- `Sfid.EntityFramework`
  Contains EF Core converters, comparers, property builder extensions, value generators, and `AssignSnowfakeKeys()`.
- `Sfid.Test`
  Covers public API behavior, edge cases, DI bootstrapping, and EF integration.
- `Sfid.Benchmark`
  Measures generation, parsing, decomposition, and EF conversion paths.

## Identifier Layout

The default layout is compatible with the classic Snowflake split:

- `41` bits for timestamp
- `5` bits for datacenter
- `5` bits for worker
- `12` bits for sequence

The library also supports expanded worker capacity through `WorkerCapacity`. When `WorkerCapacity` grows beyond the standard `32` workers, bits are borrowed from the sequence range to increase worker slots.

| WorkerCapacity | Max WorkerId | Sequence Bits | Approximate IDs/ms/worker |
| --- | --- | --- | --- |
| `32` | `31` | `12` | `4096` |
| `1024` | `1023` | `7` | `128` |
| `2048` | `2047` | `6` | `64` |
| `4096` | `4095` | `5` | `32` |
| `131072` | `131071` | `0` | `1` |

Every process that shares the same logical ID space must use the same `WorkerCapacity`, otherwise decomposition and throughput expectations will diverge.

## Core API

Create a generator explicitly:

```csharp
using Sfid.Net;

var generator = new SfidGenerator(
    new SfidOptions
    {
        DatacenterId = 1,
        WorkerId = 7,
        WorkerCapacity = 32,
        ClockRegressionTolerance = TimeSpan.FromMilliseconds(2),
    });

long rawId = generator.NextId();
Sfid sfid = generator.Next<Sfid>();
```

Define strongly typed identifiers:

```csharp
using Sfid.Net.Abstractions;

public readonly record struct OrderId(long Value) : ISfid<OrderId>
{
    public static OrderId FromInt64(long value) => new(value);
}

OrderId orderId = generator.Next<OrderId>();
```

Parse or rehydrate IDs:

```csharp
var parsed = SfidParser.Parse<OrderId>("123456789012345678");
var typed = SfidParser.FromInt64<OrderId>(123456789012345678);

if (SfidParser.TryParse<OrderId>("123456789012345678", out var result))
{
    Console.WriteLine(result.Value);
}
```

Decompose a generated ID:

```csharp
var parts = generator.Decompose(rawId);

Console.WriteLine(parts.Timestamp);
Console.WriteLine(parts.DatacenterId);
Console.WriteLine(parts.WorkerId);
Console.WriteLine(parts.Sequence);
```

## JSON and HTTP Binding

`Sfid` includes a built-in `System.Text.Json` converter, so it works out of the box in JSON request and response bodies.

If you expose custom strongly typed identifiers that implement `ISfid<TSelf>`, register the shared converter factory:

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

The JSON converters serialize IDs as strings and accept either strings or integers on input. For route and query binding, `Sfid` implements `IParsable<Sfid>` and exposes `Parse` and `TryParse`. Custom strongly typed IDs should add matching static parse methods if you want them to bind directly from route or query values in Minimal APIs or MVC.

## Runtime Bootstrap

`SfidRuntime` exposes a process-wide generator. It is convenient for small tools, local development, and applications that want a single singleton generator shared across the process.

```csharp
SfidRuntime.Bootstrap(new SfidOptions
{
    DatacenterId = 2,
    WorkerId = 5,
});

long id = SfidRuntime.NextId();
Sfid sfid = Sfid.Generate();
```

If you never bootstrap the runtime explicitly, `SfidRuntime.Current` lazily creates a default generator with `DatacenterId = 0` and `WorkerId = 0`. That fallback is appropriate for local or single-process use, but it should not be treated as a multi-instance production strategy.

## Host and Configuration Bootstrap

The DI extensions live in the `Microsoft.Extensions.DependencyInjection` namespace:

```csharp
builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
```

You can also provide application metadata manually:

```csharp
services.AddSnowfake(configuration, "orders-api", "orders-api-01");
```

Or bootstrap entirely from code:

```csharp
services.AddSnowfake(options =>
{
    options.DatacenterId = 1;
    options.WorkerId = 7;
    options.WorkerCapacity = 64;
});
```

Important notes:

- The configuration section name is currently `Snowfake`.
- `Snowfake:InstanceId` is preferred when present.
- `ServiceRuntime:InstanceId` is used as a fallback.
- When `DatacenterId` and `WorkerId` are both omitted, the library hashes application and instance metadata to derive a best-effort node slot.
- When a datacenter is fixed but the worker is omitted, the default auto-assigned capacity expands to `1024` workers.

This auto-assignment mode is convenient, but explicit infrastructure-managed worker allocation remains the safest option for large production fleets.

## Entity Framework Core

`Sfid.EntityFramework` keeps EF-specific dependencies separate from the core package.

Store identifiers as `bigint`:

```csharp
modelBuilder.Entity<OrderEntity>(entity =>
{
    entity.HasKey(order => order.Id);
    entity.Property(order => order.Id).HasSnowfakeKey();
    entity.Property(order => order.CustomerId).HasSnowfakeConversion();
});
```

Store identifiers as strings:

```csharp
entity.Property(order => order.Id).HasSnowfakeKey(SfidStorageKind.String);
```

Generate keys during save:

```csharp
public override int SaveChanges(bool acceptAllChangesOnSuccess)
{
    this.AssignSnowfakeKeys();
    return base.SaveChanges(acceptAllChangesOnSuccess);
}
```

`AssignSnowfakeKeys()` only fills properties that:

- are marked with `HasSnowfakeKey()`
- are still at their default value
- implement `ISfid<TSelf>`

If a property is misconfigured, the library now throws a clear `InvalidOperationException` instead of leaking a generic type-constraint error.

## Operational Guidance

- Use one generator instance per process.
- Keep `(DatacenterId, WorkerId)` unique across simultaneously running nodes.
- Keep system clocks synchronized.
- When using expanded worker capacity, use the same `WorkerCapacity` everywhere in the same ID space.
- Prefer explicit worker allocation for production environments with strict uniqueness guarantees.
- If JavaScript clients consume raw 64-bit IDs directly, consider serializing them as strings to avoid precision loss.

## Validation and Benchmarks

The repository ships with automated tests, benchmarks, and release tooling.

- Benchmark guide: [`docs/benchmarking.md`](docs/benchmarking.md)
- Appsettings configuration guide: [`docs/appsettings-configuration.md`](docs/appsettings-configuration.md)
- Latest verification snapshot: [`docs/verification.md`](docs/verification.md)
- NuGet publishing guide: [`docs/publishing-to-nuget.md`](docs/publishing-to-nuget.md)

## Common Commands

```bash
dotnet test Sfid.Net.slnx
dotnet run --project tests/Sfid.Benchmark/Sfid.Benchmark.csproj -c Release -- --filter *
pwsh ./eng/Pack.ps1 -Configuration Release
pwsh ./eng/Publish-NuGet.ps1 -Configuration Release -Version 0.1.0
```
