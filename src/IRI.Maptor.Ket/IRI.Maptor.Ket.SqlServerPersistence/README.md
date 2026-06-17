# IRI.Maptor.Ket.SqlServerPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.SqlServerPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8 (Windows)** persistence adapter for **SQL Server Spatial** — implements the Maptor data-source interfaces for reading and writing spatial features stored in SQL Server `geometry` / `geography` columns.

---

## Features

- `SqlServerDataSource` — implements `IVectorDataSource` / `IEditableVectorDataSource` for SQL Server spatial tables
- `SqlServerInfrastructure` — connection management, SQL helpers, and schema discovery
- `SqlServerScaleDependentDataSource` — variant that adjusts the loaded feature set based on map scale
- `SqlServerSourceParameter` — strongly-typed connection/query parameters
- Reads SQL Server native binary spatial format and converts to native `Geometry<Point>` objects
- Spatial bounding-box queries using SQL Server spatial indexes
- Write support: insert, update, delete features

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.SqlServerPersistence
```

---

## Project Structure

```
Ket.SqlServerPersistence/
├── SqlServerDataSource.cs               # IVectorDataSource / IEditableVectorDataSource
├── SqlServerInfrastructure.cs           # Connection & SQL helpers
├── SqlServerScaleDependentDataSource.cs # Scale-dependent loading
└── SqlServerSourceParameter.cs         # Connection parameters
```

---

📦 **NuGet**: [IRI.Maptor.Ket.SqlServerPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.SqlServerPersistence)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
