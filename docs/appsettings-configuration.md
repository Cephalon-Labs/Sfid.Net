# Appsettings Configuration Guide

This guide explains how to configure `Sfid.Net` from `appsettings.json`, environment-specific configuration files, and environment variables when you bootstrap the runtime with `AddSnowfake(...)`.

The examples in this document apply to both Minimal APIs and controller-based ASP.NET Core applications. The same configuration model also works in generic hosts and worker services.

## When to Use Configuration-Based Bootstrap

Use configuration-based bootstrap when you want the generator node identity to be controlled by deployment settings instead of hard-coded in `Program.cs`.

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
```

This overload reads the `Snowfake` section from configuration, resolves application and instance metadata, bootstraps `SfidRuntime`, and registers the process-wide generator as `ISfidGenerator`.

You can also pass explicit metadata:

```csharp
builder.Services.AddSnowfake(builder.Configuration, "orders-api", "orders-api-01");
```

That overload is useful when you want stable naming that does not depend on the host environment.

## Configuration Section

The configuration section name is currently `Snowfake`.

```json
{
  "Snowfake": {
    "DatacenterId": 1,
    "WorkerId": 7
  }
}
```

## Configuration Keys

| Key | Type | Required | Default or Resolution Rule | Notes |
| --- | --- | --- | --- | --- |
| `Snowfake:DatacenterId` | `int?` | No | Auto-assigned when omitted | Intended range is `0` to `31`. |
| `Snowfake:WorkerId` | `int?` | No | Auto-assigned when omitted | Must fit within the resolved `WorkerCapacity`. |
| `Snowfake:WorkerCapacity` | `int?` | No | Usually `32`; becomes `1024` when `DatacenterId` is fixed and `WorkerId` is omitted | All nodes in the same ID space should use the same value. |
| `Snowfake:CustomEpoch` | `DateTimeOffset?` | No | `2010-11-04T01:42:54.657Z` | Must not be in the future. |
| `Snowfake:ClockRegressionToleranceMilliseconds` | `int` | No | `0` | Negative values are normalized to `0`. |
| `Snowfake:InstanceId` | `string?` | No | Derived from host metadata when omitted | Overrides `ServiceRuntime:InstanceId` and host-derived values. |

## Quick Reference by Scenario

| Scenario | Recommended Settings |
| --- | --- |
| Single-process local development | Omit `DatacenterId` and `WorkerId`, or set both explicitly if you want deterministic local IDs |
| Fixed infrastructure-assigned node | Set `DatacenterId`, `WorkerId`, and `WorkerCapacity` explicitly |
| Fixed datacenter, many replicas per datacenter | Set `DatacenterId`; optionally set `WorkerCapacity`; let `WorkerId` be derived from `InstanceId` |
| Fully automatic best-effort node assignment | Omit both `DatacenterId` and `WorkerId` and let the library hash application and instance identity |
| Shared production ID space | Keep `WorkerCapacity` and `CustomEpoch` identical across every node that generates IDs |

## Core Registration Patterns

### Minimal API or MVC App

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### Worker Service or Generic Host

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();
```

### Explicit Metadata

```csharp
builder.Services.AddSnowfake(
    builder.Configuration,
    applicationName: "orders-api",
    instanceId: "orders-api-01");
```

This is the most predictable option if you want to remove host-specific naming differences from the node identity calculation.

## Appsettings Examples

### 1. Fixed Datacenter and Fixed Worker

Use this when your deployment platform assigns node slots explicitly and you want no automatic hashing.

```json
{
  "Snowfake": {
    "DatacenterId": 1,
    "WorkerId": 7,
    "WorkerCapacity": 32,
    "ClockRegressionToleranceMilliseconds": 2
  }
}
```

Behavior:

- `DatacenterId` is fixed to `1`
- `WorkerId` is fixed to `7`
- `WorkerCapacity` stays `32`
- No host-derived hashing is needed

### 2. Fixed Datacenter, Auto-Assigned Worker

Use this when each environment or region has a known datacenter slot, but individual replicas should derive their own worker IDs.

```json
{
  "Snowfake": {
    "DatacenterId": 4,
    "WorkerCapacity": 1024
  },
  "ServiceRuntime": {
    "InstanceId": "orders-api-17"
  }
}
```

Behavior:

- `DatacenterId` is fixed to `4`
- `WorkerId` is derived by hashing the resolved instance ID
- `WorkerCapacity` is `1024`
- If you omit `WorkerCapacity` in this specific scenario, the library defaults it to `1024`

This is a good fit for a production deployment where replicas are unique inside a datacenter but you do not want to assign worker IDs manually.

### 3. Fully Automatic Node Assignment

Use this for simple deployments, local development, preview environments, or any setup where best-effort uniqueness from host metadata is acceptable.

```json
{
  "Snowfake": {
  }
}
```

Behavior:

- `DatacenterId` and `WorkerId` are derived from a hashed node slot
- The node slot is based on the resolved application name and instance ID
- `WorkerCapacity` stays at the standard `32`
- The effective auto-assignment space is the classic `32 x 32 = 1024` node layout

This is convenient, but explicit node allocation is still safer for large production fleets.

### 4. Fixed Worker, Auto-Assigned Datacenter

Use this only when worker IDs are managed externally and you intentionally want datacenter identity to be derived from the application name.

```json
{
  "Snowfake": {
    "WorkerId": 12,
    "WorkerCapacity": 32
  }
}
```

Behavior:

- `WorkerId` is fixed to `12`
- `DatacenterId` is derived by hashing the resolved application name
- `WorkerCapacity` stays `32` unless you set it explicitly

This is less common than fixing the datacenter and auto-assigning the worker.

### 5. Custom Epoch

Use this only if you intentionally want an ID timeline that starts from a custom date and you can guarantee every generator in the same ID space uses the same epoch.

```json
{
  "Snowfake": {
    "DatacenterId": 2,
    "WorkerId": 55,
    "WorkerCapacity": 64,
    "CustomEpoch": "2025-01-01T00:00:00Z",
    "ClockRegressionToleranceMilliseconds": 5
  }
}
```

Behavior:

- IDs use `2025-01-01T00:00:00Z` as the zero point for the timestamp portion
- `WorkerCapacity` expands the worker bit allocation beyond the classic `32`
- Throughput per worker drops as worker capacity increases because sequence bits are reused

If two nodes share the same logical ID space, they must use the same `CustomEpoch` and `WorkerCapacity`.

### 6. Environment-Specific Overrides

Use a shared baseline in `appsettings.json` and override only environment-specific values in files such as `appsettings.Development.json` or `appsettings.Production.json`.

`appsettings.json`

```json
{
  "Snowfake": {
    "ClockRegressionToleranceMilliseconds": 2
  }
}
```

`appsettings.Production.json`

```json
{
  "Snowfake": {
    "DatacenterId": 3,
    "WorkerCapacity": 1024
  }
}
```

This keeps the deployment-specific node identity separate from the shared application defaults.

## Environment Variable Mapping

ASP.NET Core configuration maps double underscores to section separators. These values are equivalent to JSON keys:

```text
Snowfake__DatacenterId=4
Snowfake__WorkerId=7
Snowfake__WorkerCapacity=1024
Snowfake__CustomEpoch=2025-01-01T00:00:00Z
Snowfake__ClockRegressionToleranceMilliseconds=5
Snowfake__InstanceId=orders-api-17
ServiceRuntime__InstanceId=orders-api-17
```

Use `Snowfake__InstanceId` when you want a stable explicit instance ID inside configuration. Use `ServiceRuntime__InstanceId` as a generic fallback when you do not want to couple your deployment metadata to the `Snowfake` section directly.

## Application Name and Instance ID Resolution

The resolved node identity depends on both the application name and the instance ID.

### Application Name

When you call:

```csharp
builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
```

the library uses `builder.Environment.ApplicationName`.

The application name is then normalized:

- the last dotted segment is preferred
- non-alphanumeric characters become `-`
- the result is lowercased

Examples:

| Input | Resolved Application Name |
| --- | --- |
| `Orders.Api` | `api` |
| `Neza.Api` | `api` |
| `Billing-Worker` | `billing-worker` |

If you want a more explicit and stable application identity, use the overload that accepts `applicationName` directly.

### Instance ID Precedence

When you call `AddSnowfake(configuration, environment)`, the instance ID is resolved in this order:

1. `Snowfake:InstanceId`
2. `ServiceRuntime:InstanceId`
3. `POD_NAME`
4. `CONTAINER_APP_REPLICA_NAME`
5. `WEBSITE_INSTANCE_ID`
6. `HOSTNAME`
7. `COMPUTERNAME`
8. `Environment.MachineName`

When none of the explicit configuration values are present, the final generated instance identity is:

```text
<resolved-application-name>-<resolved-host-identity>-<process-id>
```

When you call `AddSnowfake(configuration, applicationName, instanceId)`, the precedence becomes:

1. `Snowfake:InstanceId`
2. the explicit `instanceId` argument
3. the same host-based fallback chain shown above

This means `Snowfake:InstanceId` always wins when it is present.

## Auto-Assignment Matrix

The following table describes the current node identity resolution behavior:

| Configured Values | Datacenter Resolution | Worker Resolution | WorkerCapacity Resolution |
| --- | --- | --- | --- |
| Neither `DatacenterId` nor `WorkerId` | Derived from a hashed node slot | Derived from the same hashed node slot | `32` |
| `DatacenterId` only | Fixed to the configured datacenter | Hashed from instance ID | Configured value or `1024` |
| `WorkerId` only | Hashed from application name | Fixed to the configured worker | Configured value or `32` |
| Both `DatacenterId` and `WorkerId` | Fixed | Fixed | Configured value or `32` |

## Normalization and Validation Rules

Configuration-based bootstrap is intentionally forgiving for most node identity values.

- `DatacenterId` is normalized into the supported range `0` to `31`
- `WorkerCapacity` is normalized into the supported range `1` to `131072`
- `WorkerId` is normalized into the supported range `0` to `WorkerCapacity - 1`
- `ClockRegressionToleranceMilliseconds` is normalized so negative values become `0`
- `CustomEpoch` is not normalized; a future timestamp still causes startup failure when the generator validates options

You should still keep your configuration explicit and valid instead of relying on normalization. Silent clamping can hide deployment mistakes.

## Recommended Production Patterns

### Pattern 1: Explicit Slot Allocation

Use this when you control infrastructure centrally and can assign each node a unique datacenter and worker slot.

```json
{
  "Snowfake": {
    "DatacenterId": 2,
    "WorkerId": 14,
    "WorkerCapacity": 32
  }
}
```

This is the most deterministic option.

### Pattern 2: Fixed Datacenter, Derived Worker per Replica

Use this when each region or cluster has a fixed datacenter slot, but replicas change dynamically.

```json
{
  "Snowfake": {
    "DatacenterId": 2,
    "WorkerCapacity": 1024
  }
}
```

Pair this with a stable replica identity such as:

- `Snowfake:InstanceId`
- `ServiceRuntime:InstanceId`
- `POD_NAME`
- `CONTAINER_APP_REPLICA_NAME`
- `WEBSITE_INSTANCE_ID`

### Pattern 3: Best-Effort Automatic Assignment

Use this for development, previews, experiments, and small deployments where host-derived uniqueness is enough.

```json
{
  "Snowfake": {
  }
}
```

This is simple, but explicit node management remains the safest option for business-critical production traffic.

## Troubleshooting

### IDs Look Duplicated Across Nodes

Check the following first:

- nodes must not share the same effective datacenter and worker combination
- all nodes in the same ID space must use the same `WorkerCapacity`
- all nodes in the same ID space must use the same `CustomEpoch`
- instance identity should be stable and unique when workers are auto-assigned

### The Generated Datacenter or Worker Is Not What You Expected

Check the following:

- whether the app is using `AddSnowfake(configuration, environment)` or the explicit metadata overload
- whether `Snowfake:InstanceId` is overriding `ServiceRuntime:InstanceId` or the explicit `instanceId` argument
- whether values were normalized because they were out of range
- whether the resolved application name was shortened from a dotted assembly name such as `Orders.Api` to `api`

### Startup Fails with a Custom Epoch Error

Make sure:

- `Snowfake:CustomEpoch` is a valid `DateTimeOffset`
- the value is not in the future
- every generator in the same ID space uses the same epoch

## Code-Only Configuration

If you do not want configuration files at all, you can still bootstrap directly in code:

```csharp
builder.Services.AddSnowfake(options =>
{
    options.DatacenterId = 1;
    options.WorkerId = 7;
    options.WorkerCapacity = 32;
    options.ClockRegressionTolerance = TimeSpan.FromMilliseconds(2);
});
```

This path is stricter than configuration binding because invalid values are validated directly during generator creation.
