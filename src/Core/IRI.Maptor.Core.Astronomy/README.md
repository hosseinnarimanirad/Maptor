# IRI.Maptor.Core.Astronomy

[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Positional astronomy for maps: astronomical time scales, sidereal time, the Sun's position, and
sunrise, sunset, solar noon and twilight at any place and date. Written from scratch in managed C#
with no third-party dependencies; it works fully offline.

## Usage

```csharp
using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Common.Primitives;

var tehran = new Point(51.3890, 35.6892);          // X = longitude, Y = latitude, degrees

var day = Sun.GetSolarDay(tehran, new DateTime(2026, 6, 21), TimeSpan.FromHours(3.5));

Console.WriteLine($"Sunrise {day.Sunrise:HH:mm}  noon {day.SolarNoon:HH:mm}  sunset {day.Sunset:HH:mm}");
Console.WriteLine($"Civil twilight {day.CivilTwilight.Rising:HH:mm}–{day.CivilTwilight.Setting:HH:mm}, day length {day.DayLength}");

var now = DateTime.UtcNow;
var sky = Sun.GetHorizontal(tehran, now);           // azimuth from north, geometric elevation
var subSolar = Sun.GetSubSolarPoint(now);           // where the Sun is at the zenith
```

- `Sun` is the entry point:
  - `GetPosition(utc)` → `SunPosition`: apparent ecliptic longitude, right ascension, declination, distance, equation of time, sub-solar point.
  - `GetHorizontal(location, utc)` → `HorizontalCoordinate`: azimuth (from north, clockwise) and geometric elevation (no refraction).
  - `GetSolarDay(location, date, utcOffset | timeZone)` → `SolarDay`: solar noon and its elevation, sunrise/sunset and their azimuths, civil/nautical/astronomical twilight, day length, polar day/night.
  - `GetAltitudeEvents(location, date, utcOffset, altitude)`: when the Sun crosses any elevation, for custom twilight definitions.
  - `GetPhase(location, utc)` → `SolarPhase`: day, civil, nautical or astronomical twilight, or night. `SolarPhases.FromElevation` gives the same for an elevation.
- `SolarDay` is the solar day whose noon falls on the requested local date. Its sunrise is the one before that noon and its sunset the one after. All times are `DateTimeOffset`s in the requested offset.
- Missing events are `null`, never NaN. `IsPolarDay`/`IsPolarNight` (and `AltitudeEvents.IsAlwaysAbove`/`IsAlwaysBelow` for each twilight) say why.
- `SunAltitudes` holds the standard elevations: −0.8333° (sunrise/sunset), −6°, −12°, −18°.
- `Time/`:
  - `AstroTime`: Julian date, Julian Ephemeris Day (TT), ΔT, Julian centuries.
  - `SiderealTime`: Greenwich mean/apparent and local apparent sidereal time, in degrees.
- `Coordinates/`: `EquatorialCoordinate`, `HorizontalCoordinate`, and `Observer.ToHorizontal`/`GetHourAngle` for any body.
- `Events/CrossingSolver`: bisection for the instant a time-dependent value crosses a threshold.

## Conventions

- Angles are degrees; locations are geographic `Point`s (X = longitude east-positive, Y = latitude).
- `DateTime` inputs are UTC; `Unspecified` is taken as UTC and `Local` is converted.
- UT1 is approximated by UTC (they differ by less than 0.9 s).

## Accuracy

- The Sun uses the low-precision theory from Meeus, *Astronomical Algorithms*, ch. 25, the same theory as NOAA's solar calculator. That is about 0.01° in position.
- Nutation is a four-term series, good to about 0.5″.
- Rise/set times are solved to 1 s. They agree with the U.S. Naval Observatory to within its minute rounding, including at polar latitudes. The unpredictable part is the atmosphere: real refraction near the horizon varies by minutes.
- ΔT uses the Espenak–Meeus polynomials, so years after 2025 are an extrapolation. This shifts the Sun by well under 0.001°.

Tests: `tests/IRI.Maptor.Tests/Astronomy` (Meeus worked examples 7.a, 12.a/b, 25.a, 28.b; USNO times for Tehran, Sydney and Tromsø).

## Room to grow

The project is meant to hold the rest of positional astronomy as it is needed:

- The Moon: position, phase, illumination, moonrise/moonset.
- Planets (truncated VSOP87) and a bright-star catalogue.
- Solar and lunar eclipses.
- Prayer times, which are sun-elevation events (`GetAltitudeEvents`) plus the Asr shadow rule.
- Shadow length and direction, and solar-panel angles.

`IRI.Maptor.Core.Satellite` builds on this project for sidereal time and the Sun.
