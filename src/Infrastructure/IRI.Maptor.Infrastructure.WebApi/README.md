# IRI.Maptor.Infrastructure.WebApi

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Infrastructure.WebApi?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.WebApi/)
[![Target](https://img.shields.io/badge/net8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Persistence adapter that loads spatial features from an HTTP Web API endpoint into an in-memory
Maptor data source, so remote feature services can be used as editable map layers without changing
the calling code. Edits are tracked locally and pushed back to a sync endpoint with optimistic
concurrency handling.

## Installation

```bash
dotnet add package IRI.Maptor.Infrastructure.WebApi
```

## Features

- `WebApiDataSource` — extends `MemoryDataSource` (an editable in-memory vector source); `LoadAsync` fetches a JSON feature-set DTO from the configured list endpoint
- Change tracking and sync: `SaveChangesAsync` pushes added/updated/deleted features to the sync endpoint, applies server-assigned ids and row versions, and throws `ConcurrencyConflictException` on conflicts
- Optional server-side geometry filter: `LoadAsync(Geometry<Point>)` sends the filter geometry as hex-encoded WKB
- In-memory attribute text search (`SearchAsync`)
- Bearer-token and custom-header authentication via `WebApiSourceParameter` (list URL, sync URL, SRID, id column)
- Optional shared `HttpClient` (`WebApiSourceParameter.HttpClient`) so many sources reuse pooled
  connections and the client's handler policies (TLS, certificate validation, proxy) instead of a
  throwaway client per request; loads are throttled library-wide and retried on transient failures,
  and a failed load surfaces as `HasError` rather than an empty layer
- `WebApiInfrastructure` — static HTTP helpers: `GetFeaturesAsync`, `SaveChangesAsync`, `AddFeatureAsync`, `UpdateFeatureAsync`, `DeleteFeatureAsync`
- `ListFeaturesQueryParams` — explicit query model for the list endpoint (`GeometryWkbHex`, `SearchText`)

## Usage

```csharp
using IRI.Maptor.Infrastructure.WebApi;

var source = new WebApiDataSource(new WebApiSourceParameter(
    listUrl: "https://example.com/api/features/list",
    syncUrl: "https://example.com/api/features/sync",
    bearerToken: token)
{
    // Optional but recommended when creating many sources: share one long-lived client so
    // connections are pooled (no TLS handshake per layer) and its TLS/proxy policies apply.
    // With a shared client, a null bearerToken uses the client's default Authorization header.
    HttpClient = sharedClient,
});

// load features from the list endpoint
await source.LoadAsync();

// ... edit features through the data source, then push the changes:
await source.SaveChangesAsync();
```

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Infrastructure.WebApi/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Infrastructure](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/Infrastructure/README.md)
