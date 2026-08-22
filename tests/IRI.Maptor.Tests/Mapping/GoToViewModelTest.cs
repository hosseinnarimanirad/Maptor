using System.Linq;

using IRI.Maptor.Presentation.Wpf.Models.GoTo;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The Go To dialog's presenter, driven without a window: every input system must resolve to
/// the same WGS 84 point, and switching between them must not move it.
/// </summary>
public class GoToViewModelTest
{

    private static void AssertClose(double expected, double actual, double tolerance)
        => Assert.InRange(actual, expected - tolerance, expected + tolerance);
    // Tehran, Azadi tower
    private const double Lat = 35.6997;
    private const double Lon = 51.3380;

    private static GoToViewModel Create() => new GoToViewModel(_ => { }, _ => { }, _ => { });

    private static void AssertPoint(GoToViewModel vm, double lat, double lon, double tolerance = 1e-7)
    {
        Assert.True(vm.IsValid, vm.ValidationMessage);
        Assert.NotNull(vm.Wgs84Point);
        AssertClose(lat, vm.Wgs84Point!.Y, tolerance);
        AssertClose(lon, vm.Wgs84Point!.X, tolerance);
    }

    [Fact]
    public void StartsValidAtOrigin()
    {
        var vm = Create();

        Assert.Equal(GoToMode.Geodetic, vm.Mode);
        Assert.Equal(GeodeticFormat.DegreesMinutesSeconds, vm.GeodeticFormat);
        AssertPoint(vm, 0, 0);
        Assert.True(vm.ZoomToCommand.CanExecute(null));
    }

    [Fact]
    public void DmsEntryResolves()
    {
        var vm = Create();

        vm.Latitude.Degrees = 35; vm.Latitude.Minutes = 41; vm.Latitude.Seconds = 58.92; vm.Latitude.Hemisphere = "N";
        vm.Longitude.Degrees = 51; vm.Longitude.Minutes = 20; vm.Longitude.Seconds = 16.8; vm.Longitude.Hemisphere = "E";

        AssertPoint(vm, Lat, Lon, 1e-6);

        vm.Latitude.Hemisphere = "S";
        AssertPoint(vm, -Lat, Lon, 1e-6);

        Assert.Contains("35°41′58.92″ S", vm.ResultDmsText);
        Assert.StartsWith("-35.699700, 51.338000", vm.ResultDecimalText);
        Assert.StartsWith("UTM 39S", vm.ResultUtmText);
    }

    [Fact]
    public void DecimalEntryAcceptsDecimalAndDmsText()
    {
        var vm = Create();
        vm.GeodeticFormat = GeodeticFormat.DecimalDegrees;

        vm.LatitudeText = "35.6997";
        vm.LongitudeText = "51.338";
        AssertPoint(vm, Lat, Lon);

        vm.LatitudeText = "35 41 58.92 N";
        vm.LongitudeText = "۵۱٫۳۳۸";
        AssertPoint(vm, Lat, Lon, 1e-6);
    }

    [Fact]
    public void SwitchingFormatCarriesTheValue()
    {
        var vm = Create();
        vm.GeodeticFormat = GeodeticFormat.DecimalDegrees;
        vm.LatitudeText = "-35.6997";
        vm.LongitudeText = "51.338";

        vm.GeodeticFormat = GeodeticFormat.DegreesMinutesSeconds;

        Assert.Equal("S", vm.Latitude.Hemisphere);
        Assert.Equal(35, vm.Latitude.Degrees);
        Assert.Equal(41, vm.Latitude.Minutes);
        AssertPoint(vm, -Lat, Lon, 1e-6);

        vm.GeodeticFormat = GeodeticFormat.DecimalDegrees;
        Assert.Equal("-35.6997", vm.LatitudeText);
    }

    [Fact]
    public void OutOfRangeInputBlocksTheActions()
    {
        var vm = Create();
        vm.GeodeticFormat = GeodeticFormat.DecimalDegrees;

        vm.LatitudeText = "95";
        Assert.False(vm.IsValid);
        Assert.NotNull(vm.ValidationMessage);
        Assert.Null(vm.Wgs84Point);
        Assert.False(vm.ZoomToCommand.CanExecute(null));
        Assert.False(vm.PanToCommand.CanExecute(null));
        Assert.False(vm.AddToDrawingCommand.CanExecute(null));
        Assert.False(vm.CopyCommand.CanExecute(null));

        vm.LatitudeText = "abc";
        Assert.False(vm.IsValid);

        vm.LatitudeText = "35";
        Assert.True(vm.IsValid);
        Assert.Null(vm.ValidationMessage);

        vm.GeodeticFormat = GeodeticFormat.DegreesMinutesSeconds;
        vm.Latitude.Minutes = 61;
        Assert.False(vm.IsValid);
        vm.Latitude.Minutes = 59;
        Assert.True(vm.IsValid);

        vm.Latitude.Degrees = 90;
        vm.Latitude.Minutes = 0;
        vm.Latitude.Seconds = 0;
        Assert.True(vm.IsValid);
        vm.Latitude.Seconds = 1;
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void MapCentrePrimesEveryTab()
    {
        var vm = Create();

        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(new Point(Lon, Lat));
        vm.SetWebMercatorPoint(webMercator);

        AssertPoint(vm, Lat, Lon, 1e-6);
        Assert.Equal("N", vm.Latitude.Hemisphere);
        Assert.Equal(35, vm.Latitude.Degrees);

        Assert.Equal(39, vm.UtmZone);
        Assert.True(vm.IsNorthernHemisphere);
        Assert.InRange(double.Parse(vm.EastingText, System.Globalization.CultureInfo.InvariantCulture), 500_000, 560_000);
        Assert.InRange(double.Parse(vm.NorthingText, System.Globalization.CultureInfo.InvariantCulture), 3_900_000, 4_000_000);

        // projected tab holds web mercator by default
        AssertClose(webMercator.X, double.Parse(vm.XText, System.Globalization.CultureInfo.InvariantCulture), 0.01);
        AssertClose(webMercator.Y, double.Parse(vm.YText, System.Globalization.CultureInfo.InvariantCulture), 0.01);
    }

    [Fact]
    public void SwitchingTabsDoesNotMoveThePoint()
    {
        var vm = Create();
        vm.SetWgs84Point(new Point(Lon, Lat));

        vm.Mode = GoToMode.Utm;
        AssertPoint(vm, Lat, Lon, 1e-7);

        vm.Mode = GoToMode.Projected;
        AssertPoint(vm, Lat, Lon, 1e-7);

        foreach (var preset in vm.Projections)
        {
            vm.SelectedProjection = preset;
            AssertPoint(vm, Lat, Lon, 1e-6);
        }

        vm.Mode = GoToMode.Geodetic;
        AssertPoint(vm, Lat, Lon, 1e-7);
    }

    [Fact]
    public void UtmSouthernHemisphereUsesTheFalseNorthing()
    {
        var vm = Create();

        // Sydney
        vm.SetWgs84Point(new Point(151.2093, -33.8688));
        vm.Mode = GoToMode.Utm;

        Assert.Equal(56, vm.UtmZone);
        Assert.False(vm.IsNorthernHemisphere);
        Assert.InRange(double.Parse(vm.NorthingText, System.Globalization.CultureInfo.InvariantCulture), 6_000_000, 6_500_000);
        AssertPoint(vm, -33.8688, 151.2093, 1e-7);

        // the same numbers read as northern hemisphere are a different place, far north
        vm.IsNorthernHemisphere = true;
        Assert.True(vm.IsValid);
        Assert.True(vm.Wgs84Point!.Y > 50);
    }

    [Fact]
    public void UtmInputResolves()
    {
        var vm = Create();
        vm.Mode = GoToMode.Utm;

        var utm = MapProjects.GeodeticToUTM(new Point(Lon, Lat), Ellipsoids.WGS84, 39, true);

        vm.UtmZone = 39;
        vm.IsNorthernHemisphere = true;
        vm.EastingText = System.Math.Round(utm.X).ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture); // "530,611"-style thousands separator
        vm.NorthingText = System.Math.Round(utm.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(vm.IsValid, vm.ValidationMessage);
        AssertClose(Lat, vm.Wgs84Point!.Y, 1e-5);   // whole-metre rounding ≈ 1e-5°
        AssertClose(Lon, vm.Wgs84Point!.X, 1e-5);
        Assert.Null(vm.WarningMessage);

        vm.EastingText = "30000";
        Assert.True(vm.IsValid);
        Assert.NotNull(vm.WarningMessage);

        vm.NorthingText = "-5";
        Assert.False(vm.IsValid);

        vm.NorthingText = "x";
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void AnotherDatumShiftsTheResult()
    {
        var vm = Create();
        vm.GeodeticFormat = GeodeticFormat.DecimalDegrees;
        vm.LatitudeText = "35.6997";
        vm.LongitudeText = "51.338";

        var onWgs84 = vm.Wgs84Point!;

        vm.Datum = vm.Ellipsoids.First(e => e.Name == Ellipsoids.Clarke1880Rgs.Name);

        Assert.True(vm.IsValid);
        var shifted = vm.Wgs84Point!;

        // a different figure of the Earth: same numbers, a different (but nearby) place
        Assert.NotEqual(onWgs84.Y, shifted.Y);
        Assert.InRange(System.Math.Abs(onWgs84.Y - shifted.Y), 1e-7, 0.05);

        // and back
        vm.Datum = vm.Ellipsoids[0];
        AssertPoint(vm, Lat, Lon);
    }

    [Fact]
    public void CustomTransverseMercatorMatchesUtm()
    {
        var vm = Create();
        vm.SetWgs84Point(new Point(Lon, Lat));

        vm.Mode = GoToMode.Utm;
        var easting = vm.EastingText;
        var northing = vm.NorthingText;

        vm.Mode = GoToMode.Projected;
        vm.SelectedProjection = vm.Projections.First(p => p.Key == "tm");

        // the TM preset defaults to zone 39's constants
        Assert.Equal(easting, vm.XText);
        Assert.Equal(northing, vm.YText);
        AssertPoint(vm, Lat, Lon, 1e-7);

        // moving the central meridian moves the easting and nothing else
        vm.CentralMeridianText = "45";
        Assert.True(vm.IsValid);
        Assert.NotEqual(Lon, vm.Wgs84Point!.X, 3);

        vm.ScaleFactorText = "abc";
        Assert.False(vm.IsValid);
        Assert.NotNull(vm.ValidationMessage);
    }

    [Fact]
    public void NamedProjectionsLockTheirDefinition()
    {
        var vm = Create();
        vm.Mode = GoToMode.Projected;

        vm.SelectedProjection = vm.Projections.First(p => p.Key == "lccNioc");

        Assert.False(vm.CanEditDatum);
        Assert.False(vm.CanEditProjectionParameters);
        Assert.True(vm.HasStandardParallels);
        Assert.Equal(Ellipsoids.Clarke1880Rgs.Name, vm.Datum.Name);
        Assert.Equal("45", vm.CentralMeridianText);
        Assert.Equal("1500000", vm.FalseEastingText);

        vm.SelectedProjection = vm.Projections.First(p => p.Key == "webMercator");
        Assert.False(vm.HasProjectionParameters);

        var webMercator = MapProjects.GeodeticWgs84ToWebMercator(new Point(Lon, Lat));
        vm.XText = webMercator.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
        vm.YText = webMercator.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

        AssertPoint(vm, Lat, Lon, 1e-7);
    }

    [Fact]
    public void QuickEntryFillsTheRightTab()
    {
        var vm = Create();

        vm.QuickEntryText = "35°41′58.92″N 51°20′16.8″E";
        Assert.True(vm.IsQuickEntryRecognized);
        Assert.Equal("35.6997, 51.338", vm.QuickEntryReading);
        Assert.Equal(GoToMode.Geodetic, vm.Mode);
        Assert.Equal("geodeticDms", vm.SelectedSystem.Key);
        AssertPoint(vm, Lat, Lon, 1e-6);

        var utm = MapProjects.GeodeticToUTM(new Point(Lon, Lat), Ellipsoids.WGS84, 39, true);
        var easting = System.Math.Round(utm.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var northing = System.Math.Round(utm.Y).ToString(System.Globalization.CultureInfo.InvariantCulture);

        vm.QuickEntryText = $"39N {easting} {northing}";
        Assert.True(vm.IsQuickEntryRecognized);
        Assert.Equal(GoToMode.Utm, vm.Mode);
        Assert.Equal("utm", vm.SelectedSystem.Key);
        Assert.Equal(39, vm.UtmZone);
        Assert.Equal(easting, vm.EastingText);
        AssertClose(Lat, vm.Wgs84Point!.Y, 1e-5);

        vm.QuickEntryText = "https://www.google.com/maps/@-33.8688,151.2093,12z";
        Assert.Equal(GoToMode.Geodetic, vm.Mode);
        AssertPoint(vm, -33.8688, 151.2093, 1e-6);

        vm.QuickEntryText = "meet me at the tower";
        Assert.True(vm.IsQuickEntryRejected);
        Assert.False(vm.IsQuickEntryRecognized);
        // the last good reading stays
        AssertPoint(vm, -33.8688, 151.2093, 1e-6);

        vm.QuickEntryText = "";
        Assert.False(vm.IsQuickEntryRejected);
        Assert.Null(vm.QuickEntryMessage);
        Assert.Null(vm.QuickEntryReading);
    }

    [Fact]
    public void SystemPickerListsEveryWayIn()
    {
        var vm = Create();

        var keys = vm.Systems.Select(o => o.Key).ToList();

        // two geodetic notations, UTM, then every projection preset in catalogue order
        Assert.Equal(new[] { "geodeticDms", "geodeticDecimal", "utm" }, keys.Take(3));
        Assert.Equal(vm.Projections.Select(p => p.Key), keys.Skip(3));
        Assert.Equal(keys.Count, keys.Distinct().Count());

        Assert.Same(vm.Systems[0], vm.SelectedSystem);
    }

    [Fact]
    public void SystemPickerDrivesModeFormatAndProjection()
    {
        var vm = Create();
        vm.SetWgs84Point(new Point(Lon, Lat));

        vm.SelectedSystem = vm.Systems.First(o => o.Key == "geodeticDecimal");
        Assert.Equal(GoToMode.Geodetic, vm.Mode);
        Assert.Equal(GeodeticFormat.DecimalDegrees, vm.GeodeticFormat);
        Assert.True(vm.ShowDecimalRows);
        Assert.False(vm.ShowDmsRows);
        AssertPoint(vm, Lat, Lon);

        vm.SelectedSystem = vm.Systems.First(o => o.Key == "utm");
        Assert.Equal(GoToMode.Utm, vm.Mode);
        Assert.True(vm.IsUtmMode);
        AssertPoint(vm, Lat, Lon, 1e-6);

        vm.SelectedSystem = vm.Systems.First(o => o.Key == "lcc");
        Assert.Equal(GoToMode.Projected, vm.Mode);
        Assert.Equal("lcc", vm.SelectedProjection.Key);
        Assert.True(vm.ShowProjectionParameters);
        AssertPoint(vm, Lat, Lon, 1e-6);

        vm.SelectedSystem = vm.Systems.First(o => o.Key == "webMercator");
        Assert.Equal("webMercator", vm.SelectedProjection.Key);
        Assert.False(vm.ShowProjectionParameters);
        AssertPoint(vm, Lat, Lon, 1e-6);

        // the inner state moves the picker too
        vm.Mode = GoToMode.Geodetic;
        Assert.Equal("geodeticDecimal", vm.SelectedSystem.Key);

        vm.GeodeticFormat = GeodeticFormat.DegreesMinutesSeconds;
        Assert.Equal("geodeticDms", vm.SelectedSystem.Key);

        vm.Mode = GoToMode.Projected;
        Assert.Equal("webMercator", vm.SelectedSystem.Key);

        vm.SelectedProjection = vm.Projections.First(p => p.Key == "tm");
        Assert.Equal("tm", vm.SelectedSystem.Key);
    }

    [Fact]
    public void DatumIsSharedAndLocksOnFixedSystems()
    {
        var vm = Create();
        vm.SetWgs84Point(new Point(Lon, Lat));

        var clarke = vm.Ellipsoids.First(e => e.Name == Ellipsoids.Clarke1880Rgs.Name);

        vm.Datum = clarke;
        Assert.Equal(clarke.Name, vm.Datum.Name);
        Assert.True(vm.CanEditDatum);

        // the same datum row serves UTM
        vm.SelectedSystem = vm.Systems.First(o => o.Key == "utm");
        Assert.Equal(clarke.Name, vm.Datum.Name);
        Assert.Contains(clarke.Name, vm.UtmZoneHint);

        // a fixed-datum system shows its own ellipsoid and refuses changes
        vm.SelectedSystem = vm.Systems.First(o => o.Key == "webMercator");
        Assert.False(vm.CanEditDatum);
        Assert.Equal(Ellipsoids.WGS84.Name, vm.Datum.Name);

        vm.Datum = clarke;
        Assert.Equal(Ellipsoids.WGS84.Name, vm.Datum.Name);

        // a free projection gets the user's choice back
        vm.SelectedSystem = vm.Systems.First(o => o.Key == "mercator");
        Assert.True(vm.CanEditDatum);
        Assert.Equal(clarke.Name, vm.Datum.Name);

        vm.SelectedSystem = vm.Systems.First(o => o.Key == "geodeticDms");
        Assert.Equal(clarke.Name, vm.Datum.Name);

        // a pasted reading is WGS 84 by convention
        vm.QuickEntryText = "35.6997, 51.338";
        Assert.Equal(Ellipsoids.WGS84.Name, vm.Datum.Name);
        AssertPoint(vm, Lat, Lon, 1e-6);
    }

    [Fact]
    public void HemisphereIndexMirrorsTheFlag()
    {
        var vm = Create();
        vm.SelectedSystem = vm.Systems.First(o => o.Key == "utm");

        Assert.Equal(0, vm.UtmHemisphereIndex);

        vm.UtmHemisphereIndex = 1;
        Assert.False(vm.IsNorthernHemisphere);

        vm.IsNorthernHemisphere = true;
        Assert.Equal(0, vm.UtmHemisphereIndex);
    }

    [Fact]
    public void EllipsoidCatalogueStartsWithWgs84AndHasNoDuplicates()
    {
        var vm = Create();

        Assert.Equal(Ellipsoids.WGS84.Name, vm.Ellipsoids[0].Name);
        Assert.Equal(Ellipsoids.GRS80.Name, vm.Ellipsoids[1].Name);
        Assert.True(vm.Ellipsoids.Count >= 15, $"only {vm.Ellipsoids.Count} ellipsoids");   // 18 are live in Ellipsoids today
        Assert.Equal(vm.Ellipsoids.Count, vm.Ellipsoids.Select(e => e.Name).Distinct().Count());
    }

    [Fact]
    public void DmsAxisNeverShowsNegativeComponents()
    {
        var axis = new DmsAxisModel(isLatitude: true) { Value = -35.6997 };

        Assert.Equal("S", axis.Hemisphere);
        Assert.Equal(35, axis.Degrees);
        Assert.Equal(41, axis.Minutes);
        AssertClose(58.92, axis.Seconds, 1e-3);
        AssertClose(-35.6997, axis.Value, 1e-9);
        Assert.True(axis.IsValid);

        axis.Value = 90;
        Assert.Equal(90, axis.Degrees);
        Assert.Equal(0, axis.Minutes);
        Assert.True(axis.IsValid);

        var lon = new DmsAxisModel(isLatitude: false) { Value = -179.999999 };
        Assert.Equal("W", lon.Hemisphere);
        Assert.Equal(179, lon.Degrees);
        Assert.Equal(59, lon.Minutes);
        Assert.True(lon.IsValid);
    }
}
