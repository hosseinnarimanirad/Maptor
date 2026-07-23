# IRI.Maptor.Ket.WindowsBase

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Ket.WindowsBase?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Ket.WindowsBase/)
[![Target](https://img.shields.io/badge/net8.0--windows-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Windows-specific helpers used by the Maptor desktop applications — hardware information queries,
Wi-Fi-based geolocation via the Google Maps Geolocation API, and a bundled managed wrapper around
the Windows Native Wi-Fi API.

## Installation

```bash
dotnet add package IRI.Maptor.Ket.WindowsBase
```

Requires Windows — depends on the Windows Native Wi-Fi API and WMI.

## Features

- `HardwareHelper` — WMI-based hardware information: processor id, HDD serial number, MAC address, mainboard and BIOS details, physical memory, CPU clock speed, default IP gateway
- `GoogleMapsGeolocationService` — geolocate the device with the Google Maps Geolocation API, using nearby Wi-Fi access points scanned via the bundled `ManagedNativeWifi` wrapper (or a caller-supplied access-point list)
- `ManagedNativeWifi` (bundled third-party component) — managed wrapper around the Windows Native Wi-Fi API for enumerating interfaces, networks, and BSS entries

## Usage

```csharp
using IRI.Maptor.Ket.WindowsBase.Services.Google;

// hardware identifiers (WMI)
string mac = HardwareHelper.GetMACAddress();
string cpuId = HardwareHelper.GetProcessorId();

// Wi-Fi based geolocation (scans nearby access points automatically)
var response = await GoogleMapsGeolocationService.GetLocationAsync(googleApiKey);
```

## Dependencies

- `ManagedNativeWifi` is vendored under `ThirdPartyLibraries/` and ships inside this package; no separate NuGet dependency is required.
- `GoogleMapsGeolocationService` requires a Google Maps Geolocation API key.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Ket.WindowsBase/) ·
[Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) ·
[Back to IRI.Maptor.Ket](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Ket/README.md)
