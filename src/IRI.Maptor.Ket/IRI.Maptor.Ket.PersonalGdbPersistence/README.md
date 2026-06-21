# IRI.Maptor.Ket.PersonalGdbPersistence

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.PersonalGdbPersistence.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8 (Windows)** persistence adapter for **ESRI Personal Geodatabases** (`.mdb` files) — accessed via OleDb / Microsoft Access Database Engine.

Geodatabases authored by any **ArcGIS 10.x or 11.x** version are supported — the version-stamped ESRI schema namespace in the geodatabase metadata is normalized automatically, so reads are independent of the authoring ArcGIS version. The adapter also picks whichever **ACE OLEDB provider** (12.0 or 16.0) is installed on the machine.

---

## Features

- `PersoanlGdbDataSource` — implements the Maptor `IVectorDataSource` interface for Personal GDB feature classes
- `PersonalGdbInfrastructure` — connection and schema helpers (open `.mdb`, list feature classes, read geometries and attributes)
- Geometry is read from the Personal GDB binary format and converted to native `Geometry<Point>` objects
- Attribute table access with type-mapped field reading

---

## Requirements

- Windows only (OleDb / ACE provider)
- Microsoft Access Database Engine (ACE OLEDB 12.0 or 16.0) must be installed

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.PersonalGdbPersistence
```

---

## Project Structure

```
Ket.PersonalGdbPersistence/
├── PersoanlGdbDataSource.cs   # IVectorDataSource implementation
├── PersonalGdbInfrastructure.cs
├── Enums/
├── Model/
└── Xml/
```

---

📦 **NuGet**: [IRI.Maptor.Ket.PersonalGdbPersistence](https://www.nuget.org/packages/IRI.Maptor.Ket.PersonalGdbPersistence)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
