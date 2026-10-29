# IRI.Maptor.Core.Satellite

[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Satellite orbits for maps:
- read published orbital elements (TLE or OMM JSON);
- propagate them with SGP4;
- get the point beneath the satellite, its height and its speed.

It is written from scratch in managed C# and references only `Core.Common` and `Core.Astronomy`. It works fully offline: the elements are the only data it needs, and they come from a file you give it.

## Usage

```csharp
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Propagation;

var errors = new List<TleReadError>();
var sets = TleReader.ReadFile(@"D:\data\stations.tle", errors.Add);   // 2- or 3-line sets; bad sets reported, not fatal

foreach (var elements in sets.Where(e => Sgp4Propagator.IsSupported(e)))
{
    var sgp4 = Sgp4Propagator.Create(elements);          // once per satellite; thread-safe afterwards
    var now = sgp4.GetPosition(DateTime.UtcNow);         // null if SGP4 reports an error (e.g. decayed)

    Console.WriteLine($"{elements.Name}: {now!.SubSatellitePoint.Y:F3}, {now.SubSatellitePoint.X:F3}, " +
                      $"{now.AltitudeKm:F0} km, elements {elements.Age(DateTime.UtcNow).TotalDays:F1} days old");
}
```

- `Elements/`
  - `OrbitalElements`: one satellite's mean elements, whether read from a TLE or an OMM. It also gives the period and `Age(at)`.
  - `TwoLineElement`: one TLE. It reads fixed columns and verifies both checksums. Two-digit epoch years follow the 1957 pivot.
  - `TleReader`: whole files with 2- and 3-line sets (the name line may start with `0 `). Comment lines (`#`) are skipped. A set that can't be read goes to `onError`; without that callback, reading throws.
  - `OmmJsonReader`: CelesTrak's GP data in JSON (CCSDS OMM).
  - `CatalogNumbers`: NORAD numbers, including Alpha-5 (`A0001` = 100001).
  - `OrbitClasses.Of(elements)`: low, medium, geosynchronous, geostationary or highly elliptical orbit. `HeightsKm` gives the perigee and apogee.
- `Propagation/`
  - `Sgp4Propagator`: SGP4, for near-Earth and deep-space orbits alike.
    - `Propagate(utc)` and `PropagateMinutes(t)` return a `PropagationResult`: TEME position (km), velocity (km/s) and an `Sgp4Status`.
    - `IsDeepSpace` (or `IsDeepSpaceOrbit(elements)` before creating one) is true for a period of 225 minutes or more: GPS, Galileo, Molniya, geostationary. These add the pull of the Moon and the Sun and the resonances of 12- and 24-hour orbits (the terms once called SDP4).
    - `Sgp4OperationMode`: `Improved` (the default, Vallado's recommendation) or `Afspc`, which reproduces the original Air Force code. It changes deep-space results only.
  - `Sgp4GravityModel`: WGS-72, the standard for published elements and the default, or WGS-84.
- `Frames/`
  - `EarthFrames`: TEME ↔ Earth-fixed, by Greenwich mean sidereal time from `Core.Astronomy`. It also gives the velocity relative to the ground.
  - `Wgs84Geodetic`: Earth-fixed ↔ latitude, longitude and height. It is exact to well below a millimetre, poles included.
- `Tracks/`
  - `GroundTrack.Compute(propagator, start, end)`: the path of the point beneath the satellite. The default sampling is 150 points per orbit. The path is cut at the 180° meridian at an interpolated crossing point, so no line streaks across the map. It stops if the satellite decays.
  - `Footprint.Of(position, minimumElevation = 10)`: the area from which the satellite is above that elevation, as a centre and an angular radius, λ = arccos(R·cos ε / (R + h)) − ε. `SphericalCapHelper` in Core.Spatial draws it.
  - `Swath.Compute(track, widthKm)`: the strip an imager covers, half the width either side of the track, as polygons clipped at ±180°. `SensorCatalog.CreateDefault()` holds known swaths: Landsat 8/9 OLI 185 km, Sentinel-2A/B/C MSI 290 km, Terra MODIS 2,330 km. Add your own with `Set`.
  - `GreatCircle` (distance, bearing, destination) and `LongitudeWindows` (splitting a shape at ±180°).
- `Sources/`
  - `CelesTrakClient`: CelesTrak GP data as OMM JSON, by group or catalog number.
  - `ElementStore`: a local copy of everything downloaded or imported.
    - It never downloads the same query twice within 2 hours, as CelesTrak asks.
    - It uses a copy younger than 12 hours without asking.
    - It falls back to the last copy when offline, and says so.
    - `Import(file)` brings TLE or OMM files into the store, for networks without internet access.
- `Passes/`
  - `PassRegion.FromPoint(p)` or `PassRegion.FromPolygon(ring)`: the place passes are found for. It also gives the great-circle distance from a point to the region (0 inside) and sample points spread over it.
  - `PassFinder.Visible(satellite, region, start, end, minimumElevation = 10)`: when the satellite is above that elevation from somewhere in the region.
  - `PassFinder.Imaged(satellite, sensor, region, start, end)`: when the imager's swath reaches the region, and `CoveredFraction`, how much of it the swath covers.
  - Each `SatellitePass` gives its start, peak and end, and the highest elevation seen from the region. For a point it also gives the rise, peak and set azimuths. It also says whether the satellite is heading north, the Sun's elevation at the region's centre (`IsDaylight`), and `IsNakedEyeVisible`: the satellite is in sunlight while the sky there is past civil dusk. That flag is never set for deep-space orbits: GPS, Galileo and geostationary satellites are far too faint to see without a telescope.
  - `StartsBeforeWindow` and `EndsAfterWindow` mark passes cut by the window. `CoversWholeWindow` is a satellite in view the whole time, such as a geostationary one. Both searches take a `CancellationToken`.
  - `LookAngles.From(place, position)`: the azimuth, elevation and range from a place on the ground. `Sunlight.IsSunlit(position)`: whether the satellite is in sunlight or in the Earth's shadow.
- `SatellitePosition` and the `GetPosition(utc)` extension give:
  - the sub-satellite point (`Point`, X = longitude, Y = latitude);
  - the altitude;
  - the orbital speed and the speed over the ground.

## Accuracy

- **SGP4 itself.** `Sgp4Propagator` is a port of David Vallado's reference implementation (version 2020-03-12), from Vallado, Crawford, Hujsak and Kelso, *Revisiting Spacetrack Report #3*, AIAA 2006-6753 (<https://celestrak.org/publications/AIAA/2006-6753/>). It is checked against the paper's verification output, in `tests/IRI.Maptor.Tests/Assets/Sgp4`:
  - **Near-Earth:** all 9 cases (158 time steps) agree to within 5 µm and 5×10⁻¹⁰ km/s, the rounding of the printed reference.
  - **Deep space:** 21 cases (454 time steps). Apart from the two below, every step agrees to within 0.2 mm in either mode. They cover 12-hour (GPS, Molniya) and 24-hour (geosynchronous) resonance, other high orbits, and a case running backwards in time. Two cases are special:
    - 23333 (WIND: eccentricity 0.97, a 13.7-day orbit) is 4 mm off in both modes. The last bits of the arithmetic matter for a nearly parabolic orbit.
    - 23599 (inclination 6.9°) agrees with AFSPC mode. In Improved mode it differs by up to 1 km, because the two modes handle the angle of the node differently at low inclinations. Elsewhere the reference follows the improved sidereal time.
- **The elements are the real limit.** SGP4 with fresh elements is good to about 1 km. Low orbits drift a few km a day as the elements age. Show the age (`OrbitalElements.Age`) and refresh elements that are more than a few days old.
- **Frames.** Polar motion is ignored, which is at most about 15 m.
- **Passes.** The search measures, at each moment, how far the point beneath the satellite is from the region. A pass is when that distance is inside the visible area, or inside half the swath width; sweeping a circle of half the width along the track gives exactly the swath strip.
  - It samples every 30 s for a low orbit, and more slowly for higher ones: a two-hundredth of the period, up to 5 minutes (`ScanStepFor`). It then refines each closest approach and each start and end to under 0.1 s. Tests check it against a second-by-second scan, for the ISS and for GPS.
  - A point uses exact look angles on the ellipsoid. A polygon uses a sphere, which puts its highest elevation within a few tenths of a degree.
  - Refraction is ignored; near the horizon it is about 0.5°. The Earth's shadow is a cylinder, so the penumbra is ignored (a few seconds at each edge).
  - One satellite over 7 days takes a few milliseconds.

## Limits

- A polygon region must not cross the ±180° meridian or contain a pole.
- **Deep-space speed:** a 12- or 24-hour orbit integrates its resonance in 720-minute steps from the epoch on every call, so a position from elements 30 days old takes about 60 steps. Every call starts from the epoch, so the propagator stays immutable and safe to share between threads, and it gives exactly the reference's numbers.
- The verification set's three deliberate error cases (33333–33335) are hand-edited TLEs with stale checksums. `TleReader` rejects them, as it should.

On a map, `SatellitesDataSource` in Core.Persistence turns all of this into one layer at its `Time`: the positions, the past and future track, the footprint and the swath. It also has `HitTest`, which finds the satellite drawn under a click, and `LabelMode`, which decides which satellites are named. In WPF, `SatelliteLayers.Create(source)` and `MapClockBinding` (Presentation.Wpf) draw that source and keep it moving.

