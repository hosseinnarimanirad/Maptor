# Satellite positions

Shows where satellites are right now. The sample reads their orbital elements and propagates each satellite with SGP4. Every few seconds it redraws the point beneath each one, on satellite imagery.

<!-- screenshot.png still to be added -->

- The sample starts with ten public TLEs from CelesTrak (`sample-2026-10-28.tle`). Eight were downloaded on 2026-10-28: the ISS, Landsat 8 and 9, Sentinel-2A/B/C, Terra and one GPS satellite (NAVSTAR 81). The other two were added on 2026-10-29: one Galileo satellite (GSAT0210) and the geostationary weather satellite Meteosat-12. TLEs age, and the sample shows how much: elements a few days old are accurate to a few km, and weeks-old ones are only a sketch.
- **Open file…** reads a TLE file (2- or 3-line sets) or CelesTrak's OMM JSON. **Paste…** takes TLEs copied from a web page. Sets that cannot be read are reported in a banner, and the rest are still shown.
- Choose a satellite in the list, or **click it on the map**, to highlight it. The pointer turns into a hand over a satellite, with its name as a tooltip. Clicking empty map keeps the choice, and Esc clears it. You see:
  - its position, altitude and orbital speed;
  - its speed over the ground and its orbital period;
  - how old its elements are.
- **Show for the chosen satellite:**
  - the ground track: one orbit behind (faint), and 1–3 orbits ahead (bright);
  - the area from which it is more than 10° above the horizon;
  - for imaging satellites, the strip the sensor covers (Landsat 185 km, Sentinel-2 290 km, Terra 2,330 km).

  The read-out also shows the visible-area radius and the sensor.
- **Names on the map:** Auto names every satellite up to 50, and only the chosen one above that. All and Chosen only are also offered.
- **Load a group** replaces the list with a whole group from CelesTrak: GPS (operational), Galileo, geostationary weather satellites, Earth resources or space stations. **The sample satellites** brings the starting list back.
- **Update from CelesTrak** downloads current elements for the listed satellites, one request per satellite, or one for the whole group when the list came from a group. It contacts celestrak.org only when pressed, and keeps a local copy in `%LocalAppData%\IRI.Maptor\Gallery\celestrak`.
  - **Within 2 hours:** pressing again uses that copy, since CelesTrak asks for at most one download every two hours.
  - **Offline:** the last copy is used, and the line under the buttons says so.
  - **Opened files** are kept in the same store.
- **The time bar** at the bottom is the same as in Day and night: Now, Live, Play at a speed, a date picker, months and a 24-hour timeline. Drag the timeline to see where a satellite was or will be. **Live positions update every** 1, 5, 10 or 30 seconds; this sets how often the clock ticks while live. Space plays and pauses.
- **Passes over a region** (the second tab of the card):
  - **Pick a region:** **Point** takes one click, **Rectangle** a drag, **Polygon** clicks on the corners (double-click to finish). The region is drawn on the map with a magenta edge.
  - **Visible** lists when each listed satellite is above the minimum elevation (0–30°) from somewhere in the region. For a point it also gives the rise and set directions. An eye icon marks passes visible to the naked eye: the satellite is in sunlight against a dark sky.
  - **Imaged** lists when the swath of Landsat, Sentinel-2 or Terra reaches the region, with the share of the region covered. A sun or moon icon shows whether the region is in daylight, which an optical imager needs.
  - The search runs from the time on the time bar, for 24 hours, 3, 7 or 14 days. It runs in the background and can be cancelled.
  - **Choose a pass:** the time bar jumps to its peak, its satellite is chosen, and the part of the track it flies is drawn in bold green.
- Every row says what kind of satellite it is: "GPS · medium orbit, 12 h", "Galileo · medium orbit, 14 h", "Geostationary", "Low orbit, 93 min". The count above the list says how many are deep space.
- **Deep-space satellites:**
  - **GPS and Galileo** have a long, wide track that repeats every day (GPS) or every ten days (Galileo), and a large visible area.
  - **A geostationary satellite** stays over one place. In the pass list it appears once, as "In view all the time", with its direction and elevation.

## What it shows

- `TleReader.Read(text, onError)` / `OmmJsonReader.Read(json, onError)` turn text into `OrbitalElements`.
- `Sgp4Propagator.IsSupported` and `Sgp4Propagator.Create` decide which satellites can be shown and propagate them.
- `propagator.GetPosition(utc)` gives a `SatellitePosition`: the sub-satellite point, altitude and speeds.
- `SatellitesDataSource` (Core.Persistence) computes everything at the clock's time on each render: the points, and for the chosen satellite `GroundTrack`, `Footprint` and `Swath`. Its `LabelMode` decides which satellites are named.
- `SatelliteLayers.Create(source)` (Presentation.Wpf) is the ready-made layer. `SatelliteLayers.HitTest(map, source, e)` finds the satellite under the mouse.
- `MapClockBinding` redraws the layer in place on each tick of the clock, as in the Day and night sample.
- `PassRegion` and `PassFinder.Visible` / `PassFinder.Imaged` (Core.Satellite) find the passes. `SatellitesDataSource.Region` and `HighlightedPass` draw them.
- `Sgp4Propagator` handles near-Earth and deep-space orbits alike. `OrbitClasses.Of(elements)` gives the kind of orbit.
- `ElementStore` with `CelesTrakQuery.Group(name)` loads a group, and with `CelesTrakQuery.CatalogNumber(n)` updates the elements, and `ElementSetResult.Source` and `Message` describe where they came from.

## The essential code

```csharp
var sets = TleReader.Read(File.ReadAllText(path), error => Debug.WriteLine(error));

var satellites = sets.Where(e => Sgp4Propagator.IsSupported(e))
                     .Select(e => Sgp4Propagator.Create(e))
                     .ToList();

foreach (var satellite in satellites)
{
    var position = satellite.GetPosition(DateTime.UtcNow);   // null if the satellite has decayed
    // position.SubSatellitePoint → a map point; position.AltitudeKm, position.SpeedKmS …
}

// on a WPF map: one layer, moved by the clock, chosen by clicking
var source = new SatellitesDataSource { Satellites = satellites };
var layer = SatelliteLayers.Create(source);
presenter.AddLayer(layer);

var binding = new MapClockBinding(presenter, layer, clock, time => source.Time = time);

// passes: visible from Tehran in the next week
var tehran = PassRegion.FromPoint(new Point(51.389, 35.689));
var passes = PassFinder.Visible(satellites[0], tehran, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
source.Region = tehran;
source.HighlightedPass = passes.FirstOrDefault();

map.PreviewMouseLeftButtonUp += (_, e) =>
{
    if (SatelliteLayers.HitTest(map, source, e) is { } hit)
    {
        source.Selected = hit;
        binding.Refresh();
    }
};
```
