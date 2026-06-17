# IRI.Maptor.Ket.WebApiPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.WebApiPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.WebApiPersistence)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8** persistence adapter that loads spatial features from an **HTTP Web API** endpoint — implements the Maptor data-source interfaces so remote feature services can be used as map layers without changing the calling code.

---

## Features

- `WebApiDataSource` — implements `IVectorDataSource` by fetching features from an HTTP endpoint (JSON/GeoJSON response)
- `WebApiInfrastructure` — HTTP client management, base URL configuration, error handling
- `WebApiSourceParameter` — strongly-typed parameters (base URL, layer name, query options)
- `ListFeaturesQueryParams` — query parameter model for feature list requests (bounding box, scale, filters)
- Bounding-box spatial filtering passed as query parameters to the server

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.WebApiPersistence
```

---

## Project Structure

```
Ket.WebApiPersistence/
├── WebApiDataSource.cs        # IVectorDataSource implementation
├── WebApiInfrastructure.cs    # HTTP client & base URL helpers
├── WebApiSourceParameter.cs   # Connection/endpoint parameters
└── ListFeaturesQueryParams.cs # Query parameter model
```

---

📦 **NuGet**: [IRI.Maptor.Ket.WebApiPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.WebApiPersistence)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
