# IRI.Maptor.Ket.PostgreSqlPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PostgreSqlPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8** persistence adapter for **PostgreSQL / PostGIS** — implements the Maptor data-source interfaces for reading and writing spatial features stored in a PostGIS database.

---

## Features

- `PostGisDataSource` — implements `IVectorDataSource` / `IEditableVectorDataSource` for PostGIS geometry columns
- `PostGisInfrastructure` — connection management, SQL helpers, and schema discovery for PostGIS tables
- Reads PostGIS geometry as WKB and converts to native `Geometry<Point>` objects
- Supports spatial queries (bounding-box filter, scale-dependent loading)
- Write support: insert, update, delete features

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.PostgreSqlPersistence
```

---

## Project Structure

```
Ket.PostgreSqlPersistence/
├── PostGisDataSource.cs        # IVectorDataSource / IEditableVectorDataSource
└── PostGisInfrastructure.cs    # Connection & SQL helpers
```

---

📦 **NuGet**: [IRI.Maptor.Ket.PostgreSqlPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PostgreSqlPersistence)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
