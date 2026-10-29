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
  and a failed load surfaces as `HasError` rather than an empty layer. With a shared client the
  feature set is parsed straight from the response stream (`HttpTransport.GetJsonStreamAsync`),
  never read into one string, and each load writes its fetch/convert times to `Trace`
- Optional compact list format (`WebApiSourceParameter.PreferCompactFormat`, off by default, shared
  client only): asks for `application/vnd.maptor.featureset.compact+json` (`CompactFeatureSetDto`
  in Core.Spatial: attribute names once, each feature a positional array). A server that does not
  know it answers with plain JSON, which is still read; both give the same `FeatureSetDto`, values
  with the same .NET types. The server side is an output formatter that answers only clients that
  ask for it (Gas: `CompactFeatureSetOutputFormatter`); add the media type to the server's
  response-compression MIME types, which do not include it by default
- Live mode (`WebApiSourceParameter.LoadMode = Live`, with `QueryUrl` = `{base}/LayerQuery/{layerSettingId}` and a
  shared client; otherwise the layer loads like Cache): `LoadAsync` only reads the fields, and every read fetches
  the web-mercator tiles covering it (`POST LayerQuery`, extent mode), once per tile, a loaded tile also covering
  its smaller tiles. Fetched features are only ever added, by id, to the one feature set that edits and
  `SaveChangesAsync` use, so pending edits survive panning and zooming and are saved wherever they are; merges run
  on the creating (UI) thread. A tile beyond the server's cap is kept but not counted as loaded
  (`HasTruncatedLiveTiles`). See `WebApiDataSource.Live.cs`. Server-side queries of a Live layer
  (`IServerQueryableVectorDataSource`): `QueryPageAsync` (attribute table), `GetDistinctValuesAsync`, and since
  2026-10-23 `GetFieldStatisticsAsync` / `GetGroupStatisticsAsync` / `GetGeometryStatisticsAsync`
  (`GET {QueryUrl}/Statistics/Field/{field}`, `/Statistics/Group/{field}?sumField=&measure=`,
  `/Statistics/Geometry?measure=`), all GETs going through one helper that throws with the server's message
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
