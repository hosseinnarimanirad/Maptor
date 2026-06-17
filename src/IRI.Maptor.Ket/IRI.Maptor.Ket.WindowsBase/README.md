# IRI.Maptor.Ket.WindowsBase

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.WindowsBase.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Ket.WindowsBase)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)

A **.NET 8 (Windows)** utility library providing Windows-specific helpers used by the Maptor desktop applications — hardware detection, geolocation services, and Wi-Fi network access.

---

## Features

- **`HardwareHelper`** — query local hardware information (e.g. machine ID, hardware fingerprinting used for licensing)
- **`GoogleMapsGeolocation`** — geolocate the device using the Google Maps Geolocation API (Wi-Fi / cell-tower based)
- **`ManagedNativeWifi`** (third-party) — managed wrapper around the Windows Native Wi-Fi API for scanning nearby access points; used as input for geolocation

---

## Installation

```bash
dotnet add package IRI.Maptor.Ket.WindowsBase
```

> Requires Windows — depends on Windows Native Wi-Fi API and Windows hardware APIs.

---

## Project Structure

```
Ket.WindowsBase/
├── Helpers/
│   └── HardwareHelper.cs
├── Services/
│   └── Google/
│       └── GoogleMapsGeolocation.cs
└── ThirdPartyLibraries/
    └── ManagedNativeWifi/   # Third-party: Windows Native Wi-Fi API wrapper
```

---

📦 **NuGet**: [IRI.Maptor.Ket.WindowsBase](https://www.nuget.org/packages/IRI.Maptor.Ket.WindowsBase)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
