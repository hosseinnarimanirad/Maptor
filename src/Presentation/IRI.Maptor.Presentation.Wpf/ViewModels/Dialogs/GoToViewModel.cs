using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Models.GoTo;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;
using Clipboard = System.Windows.Clipboard;

namespace IRI.Maptor.Presentation.Wpf.ViewModels;

/// <summary>
/// Presenter of the Go To dialog. Whatever the user types — geodetic in DMS or decimal on
/// any datum, UTM on any datum, or a named / parametrised map projection — is resolved to
/// one WGS 84 position, which is what the map actions receive and what the position line
/// echoes back.
///
/// One picker, <see cref="SelectedSystem"/>, chooses the input system; underneath it the
/// state is still <see cref="Mode"/> + <see cref="GeodeticFormat"/> + <see cref="SelectedProjection"/>,
/// and only the active mode is authoritative. Switching system re-expresses the current
/// position in the new system, so its fields are never stale, but there is no continuous
/// cross-mode write-back: that is what made the original version loop.
/// </summary>
public class GoToViewModel : Notifier
{
    private const double UtmSouthFalseNorthing = 10_000_000;

    private readonly Action<Point> _requestPanTo;
    private readonly Action<Point> _requestZoomTo;
    private readonly Action<Point> _requestAddToDrawingItem;

    private bool _isSyncing;

    private Point? _wgs84Point;

    #region Construction

    public GoToViewModel(
        Action<Point> requestPanTo,
        Action<Point> requestZoomTo,
        Action<Point> requestAddToDrawingItem)
    {
        _requestPanTo = requestPanTo;
        _requestZoomTo = requestZoomTo;
        _requestAddToDrawingItem = requestAddToDrawingItem;

        Ellipsoids = LoadEllipsoids();
        _userDatum = Ellipsoids[0];
        Projections = ProjectionPreset.CreateDefaults();

        Latitude = new DmsAxisModel(isLatitude: true);
        Longitude = new DmsAxisModel(isLatitude: false);

        Latitude.ValueChanged += (_, _) => Recalculate();
        Longitude.ValueChanged += (_, _) => Recalculate();

        _selectedProjection = Projections[0];
        ApplyProjectionDefaults(_selectedProjection);

        Systems = CoordinateSystemOption.CreateDefaults(Projections);
        _selectedSystem = Systems[0];

        Recalculate();
    }

    public static GoToViewModel Create(MapViewModelBase mapPresenter)
    {
        return new GoToViewModel(
            p =>
            {
                var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(p);

                mapPresenter.PanTo(webMercatorPoint, () => mapPresenter.FlashPoint(webMercatorPoint));
            },
            p =>
            {
                var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(p);

                mapPresenter.ZoomAndCenterToGoogleZoomLevel(13, webMercatorPoint, () => mapPresenter.FlashPoint(webMercatorPoint));
            },
            p =>
            {
                var webMercatorPoint = MapProjects.GeodeticWgs84ToWebMercator(p);

                // The drawing legend keeps its items in the map's own srs, so the point goes in as
                // web mercator; the label keeps the geodetic reading the user actually typed.
                var geometry = Geometry<Point>.Create(webMercatorPoint.X, webMercatorPoint.Y, SridHelper.WebMercator);

                var name = string.Format(
                    LocalizationManager.Instance["dialog_goto_drawingItemName"],
                    p.X.ToString("N5", CultureInfo.InvariantCulture),
                    p.Y.ToString("N5", CultureInfo.InvariantCulture));

                mapPresenter.AddDrawingItem(geometry, name);

                mapPresenter.FlashPoint(webMercatorPoint);
            });
    }

    private static List<Ellipsoid> LoadEllipsoids()
    {
        var all = typeof(Ellipsoids)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Ellipsoid))
            .Select(p => (Ellipsoid)p.GetValue(null)!)
            .ToList();

        // the two everyone reaches for first, then the rest alphabetically
        var wgs84 = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84;
        var grs80 = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.GRS80;

        var result = new List<Ellipsoid> { wgs84, grs80 };

        result.AddRange(all
            .Where(e => e.Name != wgs84.Name && e.Name != grs80.Name)
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase));

        return result;
    }

    #endregion

    #region Mode

    private GoToMode _mode = GoToMode.Geodetic;
    public GoToMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
                return;

            _mode = value;

            RaiseModeChanged();

            // carry the position over into the system that was just chosen
            if (_wgs84Point is not null)
                FillFieldsFrom(_wgs84Point, value);

            Recalculate();
        }
    }

    public bool IsGeodeticMode => Mode == GoToMode.Geodetic;

    public bool IsUtmMode => Mode == GoToMode.Utm;

    public bool IsProjectedMode => Mode == GoToMode.Projected;

    /// <summary>The DMS latitude / longitude rows are on screen.</summary>
    public bool ShowDmsRows => IsGeodeticMode && GeodeticFormat == GeodeticFormat.DegreesMinutesSeconds;

    /// <summary>The decimal latitude / longitude rows are on screen.</summary>
    public bool ShowDecimalRows => IsGeodeticMode && GeodeticFormat == GeodeticFormat.DecimalDegrees;

    /// <summary>The projection-constants grid is on screen.</summary>
    public bool ShowProjectionParameters => IsProjectedMode && SelectedProjection.HasParameters;

    private CoordinateSystemOption _selectedSystem;
    /// <summary>
    /// The one picker on the screen. Setting it drives <see cref="Mode"/>,
    /// <see cref="GeodeticFormat"/> and <see cref="SelectedProjection"/>; setting any of those
    /// (the tests and the quick-entry parser do) moves the picker to the matching entry.
    /// </summary>
    public CoordinateSystemOption SelectedSystem
    {
        get => _selectedSystem;
        set
        {
            if (value is null || ReferenceEquals(_selectedSystem, value))
                return;

            _isApplyingSystem = true;
            try
            {
                // projection and format first, so the mode switch below re-expresses the
                // position with the definition the user actually picked
                if (value.Projection is not null)
                    SelectedProjection = value.Projection;

                if (value.Mode == GoToMode.Geodetic)
                    GeodeticFormat = value.Format;

                Mode = value.Mode;
            }
            finally
            {
                _isApplyingSystem = false;
            }

            SyncSelectedSystem();
        }
    }

    private bool _isApplyingSystem;

    /// <summary>Points the picker at the entry that describes the current mode / format / projection.</summary>
    private void SyncSelectedSystem()
    {
        if (_isApplyingSystem)
            return;

        var match = Systems.FirstOrDefault(o => o.Matches(Mode, GeodeticFormat, SelectedProjection));

        if (match is null || ReferenceEquals(_selectedSystem, match))
            return;

        _selectedSystem = match;
        RaisePropertyChanged(nameof(SelectedSystem));
    }

    #endregion

    #region Shared lists

    public List<Ellipsoid> Ellipsoids { get; }

    public List<ProjectionPreset> Projections { get; }

    public List<CoordinateSystemOption> Systems { get; }

    #endregion

    #region Quick entry

    private string _quickEntryText = string.Empty;
    /// <summary>
    /// Free-text box above the tabs: anything <see cref="CoordinateTextParser"/> understands
    /// (decimal, DMS, hemisphere letters, Persian digits, map URLs, "39N 534123 3950123")
    /// is pushed into the matching tab as the user types.
    /// </summary>
    public string QuickEntryText
    {
        get => _quickEntryText;
        set
        {
            if (_quickEntryText == value)
                return;

            _quickEntryText = value ?? string.Empty;
            RaisePropertyChanged();

            ApplyQuickEntry();
        }
    }

    private string? _quickEntryMessage;
    public string? QuickEntryMessage
    {
        get => _quickEntryMessage;
        private set
        {
            if (_quickEntryMessage == value)
                return;

            _quickEntryMessage = value;
            RaisePropertyChanged();
        }
    }

    private string? _quickEntryReading;
    /// <summary>The normalized reading the parser produced, Latin digits, for display beside <see cref="QuickEntryMessage"/>.</summary>
    public string? QuickEntryReading
    {
        get => _quickEntryReading;
        private set
        {
            if (_quickEntryReading == value)
                return;

            _quickEntryReading = value;
            RaisePropertyChanged();
        }
    }

    private bool _isQuickEntryRecognized;
    public bool IsQuickEntryRecognized
    {
        get => _isQuickEntryRecognized;
        private set
        {
            if (_isQuickEntryRecognized == value)
                return;

            _isQuickEntryRecognized = value;
            RaisePropertyChanged();
        }
    }

    private bool _isQuickEntryRejected;
    public bool IsQuickEntryRejected
    {
        get => _isQuickEntryRejected;
        private set
        {
            if (_isQuickEntryRejected == value)
                return;

            _isQuickEntryRejected = value;
            RaisePropertyChanged();
        }
    }

    private void ApplyQuickEntry()
    {
        var text = QuickEntryText;

        if (string.IsNullOrWhiteSpace(text))
        {
            IsQuickEntryRecognized = false;
            IsQuickEntryRejected = false;
            QuickEntryMessage = null;
            QuickEntryReading = null;
            return;
        }

        if (CoordinateTextParser.TryParseUtm(text, out var zone, out var isNorth, out var easting, out var northing))
        {
            _isSyncing = true;
            try
            {
                UtmZone = zone;
                IsNorthernHemisphere = isNorth;
                EastingText = FormatMetres(easting);
                NorthingText = FormatMetres(northing);
            }
            finally
            {
                _isSyncing = false;
            }

            // a pasted reading is WGS 84 by convention (map links always are)
            _userDatum = Ellipsoids[0];

            // Mode's setter would overwrite the fields we just filled from the previous point
            _mode = GoToMode.Utm;
            RaiseModeChanged();
            Recalculate();

            ReportQuickEntry(recognized: true, FormattableString.Invariant($"UTM {zone}{(isNorth ? "N" : "S")}  {FormatMetres(easting)} E  {FormatMetres(northing)} N"));
            return;
        }

        if (CoordinateTextParser.TryParseLatLong(text, out var latitude, out var longitude))
        {
            _isSyncing = true;
            try
            {
                Latitude.Value = latitude;
                Longitude.Value = longitude;
                LatitudeText = FormatDegrees(latitude);
                LongitudeText = FormatDegrees(longitude);
            }
            finally
            {
                _isSyncing = false;
            }

            _userDatum = Ellipsoids[0];

            _mode = GoToMode.Geodetic;
            RaiseModeChanged();
            Recalculate();

            ReportQuickEntry(recognized: true, FormattableString.Invariant($"{FormatDegrees(latitude)}, {FormatDegrees(longitude)}"));
            return;
        }

        ReportQuickEntry(recognized: false, null);
    }

    private void ReportQuickEntry(bool recognized, string? reading)
    {
        IsQuickEntryRecognized = recognized;
        IsQuickEntryRejected = !recognized;

        // The reading is exposed separately and the view renders it as its own left-to-right
        // element: digits that follow Arabic-script letters inside one string are laid out
        // right-to-left by the bidi algorithm, which would swap "35.69, 51.33" around.
        QuickEntryReading = recognized ? reading : null;

        QuickEntryMessage = recognized
            ? LocalizationManager.Instance["dialog_goto_quickEntryRecognized"]
            : LocalizationManager.Instance["dialog_goto_quickEntryNotRecognized"];
    }

    private void RaiseModeChanged()
    {
        RaisePropertyChanged(nameof(Mode));
        RaisePropertyChanged(nameof(IsGeodeticMode));
        RaisePropertyChanged(nameof(IsUtmMode));
        RaisePropertyChanged(nameof(IsProjectedMode));
        RaisePropertyChanged(nameof(ShowDmsRows));
        RaisePropertyChanged(nameof(ShowDecimalRows));
        RaisePropertyChanged(nameof(ShowProjectionParameters));

        // the datum row locks on some projections only
        RaiseDatumChanged();

        SyncSelectedSystem();
    }

    #endregion

    #region Geodetic

    private GeodeticFormat _geodeticFormat = GeodeticFormat.DegreesMinutesSeconds;
    public GeodeticFormat GeodeticFormat
    {
        get => _geodeticFormat;
        set
        {
            if (_geodeticFormat == value)
                return;

            // keep the representation the user is about to see in step with the one they leave
            if (value == GeodeticFormat.DecimalDegrees && Latitude.IsValid && Longitude.IsValid)
            {
                _isSyncing = true;
                LatitudeText = FormatDegrees(Latitude.Value);
                LongitudeText = FormatDegrees(Longitude.Value);
                _isSyncing = false;
            }
            else if (value == GeodeticFormat.DegreesMinutesSeconds
                     && CoordinateTextParser.TryParseAngle(LatitudeText, out var lat)
                     && CoordinateTextParser.TryParseAngle(LongitudeText, out var lon))
            {
                _isSyncing = true;
                Latitude.Value = lat;
                Longitude.Value = lon;
                _isSyncing = false;
            }

            _geodeticFormat = value;

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDmsFormat));
            RaisePropertyChanged(nameof(IsDecimalFormat));
            RaisePropertyChanged(nameof(ShowDmsRows));
            RaisePropertyChanged(nameof(ShowDecimalRows));

            SyncSelectedSystem();

            Recalculate();
        }
    }

    public bool IsDmsFormat
    {
        get => GeodeticFormat == GeodeticFormat.DegreesMinutesSeconds;
        set { if (value) GeodeticFormat = GeodeticFormat.DegreesMinutesSeconds; }
    }

    public bool IsDecimalFormat
    {
        get => GeodeticFormat == GeodeticFormat.DecimalDegrees;
        set { if (value) GeodeticFormat = GeodeticFormat.DecimalDegrees; }
    }

    public DmsAxisModel Latitude { get; }

    public DmsAxisModel Longitude { get; }

    private string _latitudeText = "0";
    /// <summary>Decimal-degree entry; also accepts any single-angle notation the parser knows.</summary>
    public string LatitudeText
    {
        get => _latitudeText;
        set
        {
            if (_latitudeText == value)
                return;

            _latitudeText = value ?? string.Empty;
            RaisePropertyChanged();
            Recalculate();
        }
    }

    private string _longitudeText = "0";
    public string LongitudeText
    {
        get => _longitudeText;
        set
        {
            if (_longitudeText == value)
                return;

            _longitudeText = value ?? string.Empty;
            RaisePropertyChanged();
            Recalculate();
        }
    }

    #endregion

    #region UTM

    private int _utmZone = 39;
    public int UtmZone
    {
        get => _utmZone;
        set
        {
            var clamped = Math.Max(1, Math.Min(60, value));

            if (_utmZone == clamped)
                return;

            _utmZone = clamped;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(UtmZoneHint));
            Recalculate();
        }
    }

    private bool _isNorthernHemisphere = true;
    public bool IsNorthernHemisphere
    {
        get => _isNorthernHemisphere;
        set
        {
            if (_isNorthernHemisphere == value)
                return;

            _isNorthernHemisphere = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsSouthernHemisphere));
            RaisePropertyChanged(nameof(UtmHemisphereIndex));
            RaisePropertyChanged(nameof(UtmZoneHint));
            Recalculate();
        }
    }

    public bool IsSouthernHemisphere
    {
        get => !IsNorthernHemisphere;
        set => IsNorthernHemisphere = !value;
    }

    /// <summary>0 = northern, 1 = southern; what the hemisphere combo binds to.</summary>
    public int UtmHemisphereIndex
    {
        get => IsNorthernHemisphere ? 0 : 1;
        set => IsNorthernHemisphere = value != 1;
    }

    private string _eastingText = "500000";
    public string EastingText
    {
        get => _eastingText;
        set
        {
            if (_eastingText == value)
                return;

            _eastingText = value ?? string.Empty;
            RaisePropertyChanged();
            Recalculate();
        }
    }

    private string _northingText = "0";
    public string NorthingText
    {
        get => _northingText;
        set
        {
            if (_northingText == value)
                return;

            _northingText = value ?? string.Empty;
            RaisePropertyChanged();
            Recalculate();
        }
    }

    /// <summary>"51° E · EPSG:32639" — the EPSG code only exists on WGS 84; other datums show their name instead.</summary>
    public string UtmZoneHint
    {
        get
        {
            var centralMeridian = MapProjects.CalculateCentralMeridian(UtmZone);

            var meridian = FormattableString.Invariant($"{Math.Abs(centralMeridian)}° {(centralMeridian < 0 ? "W" : "E")}");

            var epsg = Datum.AreTheSame(IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84)
                ? FormattableString.Invariant($"EPSG:{UTM.GetSrid(UtmZone, IsNorthernHemisphere)}")
                : Datum.Name;

            // the view prefixes the localized "Central meridian" label as a separate element
            return meridian + " · " + epsg;
        }
    }

    #endregion

    #region Projected

    private ProjectionPreset _selectedProjection;
    public ProjectionPreset SelectedProjection
    {
        get => _selectedProjection;
        set
        {
            if (value is null || ReferenceEquals(_selectedProjection, value))
                return;

            _selectedProjection = value;

            ApplyProjectionDefaults(value);

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CanEditProjectionParameters));
            RaisePropertyChanged(nameof(HasProjectionParameters));
            RaisePropertyChanged(nameof(HasStandardParallels));
            RaisePropertyChanged(nameof(HasScaleFactor));
            RaisePropertyChanged(nameof(ShowProjectionParameters));

            RaiseDatumChanged();
            SyncSelectedSystem();

            // the same spot, re-expressed in the newly chosen system
            if (_wgs84Point is not null)
                FillFieldsFrom(_wgs84Point, GoToMode.Projected);

            Recalculate();
        }
    }

    public bool CanEditProjectionParameters => SelectedProjection.AllowsParameters;

    public bool HasProjectionParameters => SelectedProjection.HasParameters;

    public bool HasStandardParallels => SelectedProjection.HasStandardParallels;

    public bool HasScaleFactor => SelectedProjection.HasScaleFactor;

    private string _xText = "0";
    public string XText
    {
        get => _xText;
        set
        {
            if (_xText == value)
                return;

            _xText = value ?? string.Empty;
            RaisePropertyChanged();
            Recalculate();
        }
    }

    private string _yText = "0";
    public string YText
    {
        get => _yText;
        set
        {
            if (_yText == value)
                return;

            _yText = value ?? string.Empty;
            RaisePropertyChanged();
            Recalculate();
        }
    }

    private string _centralMeridianText = "0";
    public string CentralMeridianText
    {
        get => _centralMeridianText;
        set { if (_centralMeridianText != value) { _centralMeridianText = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private string _latitudeOfOriginText = "0";
    public string LatitudeOfOriginText
    {
        get => _latitudeOfOriginText;
        set { if (_latitudeOfOriginText != value) { _latitudeOfOriginText = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private string _scaleFactorText = "1";
    public string ScaleFactorText
    {
        get => _scaleFactorText;
        set { if (_scaleFactorText != value) { _scaleFactorText = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private string _falseEastingText = "0";
    public string FalseEastingText
    {
        get => _falseEastingText;
        set { if (_falseEastingText != value) { _falseEastingText = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private string _falseNorthingText = "0";
    public string FalseNorthingText
    {
        get => _falseNorthingText;
        set { if (_falseNorthingText != value) { _falseNorthingText = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private string _standardParallel1Text = "0";
    public string StandardParallel1Text
    {
        get => _standardParallel1Text;
        set { if (_standardParallel1Text != value) { _standardParallel1Text = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private string _standardParallel2Text = "0";
    public string StandardParallel2Text
    {
        get => _standardParallel2Text;
        set { if (_standardParallel2Text != value) { _standardParallel2Text = value ?? string.Empty; RaisePropertyChanged(); Recalculate(); } }
    }

    private void ApplyProjectionDefaults(ProjectionPreset preset)
    {
        var wasSyncing = _isSyncing;
        _isSyncing = true;

        try
        {
            var p = preset.Defaults;

            CentralMeridianText = FormatDegrees(p.CentralMeridian);
            LatitudeOfOriginText = FormatDegrees(p.LatitudeOfOrigin);
            ScaleFactorText = p.ScaleFactor.ToString("0.##########", CultureInfo.InvariantCulture);
            FalseEastingText = FormatMetres(p.FalseEasting);
            FalseNorthingText = FormatMetres(p.FalseNorthing);
            StandardParallel1Text = FormatDegrees(p.StandardParallel1);
            StandardParallel2Text = FormatDegrees(p.StandardParallel2);
        }
        finally
        {
            _isSyncing = wasSyncing;
        }
    }

    private bool TryGetProjectionParameters(out ProjectionParameters parameters, out string? error)
    {
        error = null;

        if (!SelectedProjection.AllowsParameters)
        {
            parameters = SelectedProjection.Defaults;
            return true;
        }

        parameters = ProjectionParameters.Empty;

        if (!TryParseField(CentralMeridianText, "dialog_goto_paramCentralMeridian", out var cm, out error)) return false;
        if (!TryParseField(LatitudeOfOriginText, "dialog_goto_paramLatitudeOfOrigin", out var lat0, out error)) return false;
        if (!TryParseField(FalseEastingText, "dialog_goto_paramFalseEasting", out var fe, out error)) return false;
        if (!TryParseField(FalseNorthingText, "dialog_goto_paramFalseNorthing", out var fn, out error)) return false;

        double k0 = 1, sp1 = 0, sp2 = 0;

        if (SelectedProjection.HasScaleFactor && !TryParseField(ScaleFactorText, "dialog_goto_paramScaleFactor", out k0, out error)) return false;

        if (SelectedProjection.HasStandardParallels)
        {
            if (!TryParseField(StandardParallel1Text, "dialog_goto_paramStandardParallel1", out sp1, out error)) return false;
            if (!TryParseField(StandardParallel2Text, "dialog_goto_paramStandardParallel2", out sp2, out error)) return false;
        }

        parameters = new ProjectionParameters(cm, lat0, k0, fe, fn, sp1, sp2);
        return true;
    }

    #endregion

    #region Datum

    private Ellipsoid _userDatum;

    /// <summary>
    /// The ellipsoid the typed coordinates refer to, shared by every system. While a
    /// fixed-datum projection is selected (Web Mercator, the named Lambert grids) it reads
    /// as that projection's ellipsoid and cannot be changed; the user's own choice is kept
    /// and comes back when they move to a free system.
    /// </summary>
    public Ellipsoid Datum
    {
        get => IsDatumLocked ? CatalogueEntryFor(SelectedProjection.DefaultEllipsoid) : _userDatum;
        set
        {
            if (IsDatumLocked || value.Name is null || _userDatum.Equals(value))
                return;

            // the typed numbers keep their meaning on the new datum; the WGS 84 result moves
            _userDatum = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(UtmZoneHint));
            Recalculate();
        }
    }

    /// <summary>True while the selected system carries its own datum.</summary>
    public bool IsDatumLocked => Mode == GoToMode.Projected && !SelectedProjection.AllowsEllipsoid;

    public bool CanEditDatum => !IsDatumLocked;

    private void RaiseDatumChanged()
    {
        RaisePropertyChanged(nameof(Datum));
        RaisePropertyChanged(nameof(IsDatumLocked));
        RaisePropertyChanged(nameof(CanEditDatum));
        RaisePropertyChanged(nameof(UtmZoneHint));
    }

    /// <summary>The catalogue instance that equals <paramref name="ellipsoid"/>, so the combo can select it; the ellipsoid itself if none does.</summary>
    private Ellipsoid CatalogueEntryFor(Ellipsoid ellipsoid)
    {
        var match = Ellipsoids.FirstOrDefault(e => e.AreTheSame(ellipsoid) && e.Name == ellipsoid.Name);

        return match.Name is null ? ellipsoid : match;
    }

    #endregion

    #region Result

    /// <summary>The resolved WGS 84 position (X = longitude, Y = latitude), or null while the input is invalid.</summary>
    public Point? Wgs84Point => _wgs84Point;

    private bool _isValid;
    public bool IsValid
    {
        get => _isValid;
        private set
        {
            if (_isValid == value)
                return;

            _isValid = value;
            RaisePropertyChanged();
        }
    }

    private string? _validationMessage;
    /// <summary>Why the current input cannot be resolved; null when it can.</summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value)
                return;

            _validationMessage = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasValidationMessage));
        }
    }

    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

    private string? _warningMessage;
    /// <summary>Something worth a second look that does not block the action (e.g. an easting far outside its zone).</summary>
    public string? WarningMessage
    {
        get => _warningMessage;
        private set
        {
            if (_warningMessage == value)
                return;

            _warningMessage = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasWarningMessage));
        }
    }

    public bool HasWarningMessage => !string.IsNullOrEmpty(WarningMessage);

    private string _resultDecimalText = string.Empty;
    /// <summary>"35.689200, 51.389000" — what Copy puts on the clipboard.</summary>
    public string ResultDecimalText
    {
        get => _resultDecimalText;
        private set
        {
            if (_resultDecimalText == value)
                return;

            _resultDecimalText = value;
            RaisePropertyChanged();
        }
    }

    private string _resultDmsText = string.Empty;
    public string ResultDmsText
    {
        get => _resultDmsText;
        private set
        {
            if (_resultDmsText == value)
                return;

            _resultDmsText = value;
            RaisePropertyChanged();
        }
    }

    private string _resultUtmText = string.Empty;
    public string ResultUtmText
    {
        get => _resultUtmText;
        private set
        {
            if (_resultUtmText == value)
                return;

            _resultUtmText = value;
            RaisePropertyChanged();
        }
    }

    private void Recalculate()
    {
        if (_isSyncing)
            return;

        string? warning = null;

        if (TryResolveWgs84(out var point, out var error, ref warning))
        {
            _wgs84Point = point;

            ResultDecimalText = FormattableString.Invariant($"{point.Y:0.000000}, {point.X:0.000000}");

            ResultDmsText = DegreeHelper.ToDmsWithHemisphere(point.Y, isLatitude: true) + "   " + DegreeHelper.ToDmsWithHemisphere(point.X, isLatitude: false);

            ResultUtmText = FormatUtm(point);

            ValidationMessage = null;
            WarningMessage = warning;
            IsValid = true;
        }
        else
        {
            _wgs84Point = null;

            ValidationMessage = error;
            WarningMessage = null;
            IsValid = false;
        }

        RaisePropertyChanged(nameof(Wgs84Point));
    }

    private bool TryResolveWgs84(out Point wgs84, out string? error, ref string? warning)
    {
        wgs84 = null!;
        error = null;

        try
        {
            switch (Mode)
            {
                case GoToMode.Geodetic:
                    return TryResolveGeodetic(out wgs84, out error);

                case GoToMode.Utm:
                    return TryResolveUtm(out wgs84, out error, ref warning);

                case GoToMode.Projected:
                    return TryResolveProjected(out wgs84, out error);

                default:
                    return false;
            }
        }
        catch (Exception)
        {
            error = LocalizationManager.Instance["dialog_goto_errorProjection"];
            return false;
        }
    }

    private bool TryResolveGeodetic(out Point wgs84, out string? error)
    {
        wgs84 = null!;
        error = null;

        double latitude, longitude;

        if (GeodeticFormat == GeodeticFormat.DegreesMinutesSeconds)
        {
            if (!Latitude.IsValid || !Longitude.IsValid)
            {
                var axis = !Latitude.IsValid ? Latitude : Longitude;

                bool componentsOutOfRange = axis.Minutes < 0 || axis.Minutes >= 60 || axis.Seconds < 0 || axis.Seconds >= 60;

                error = componentsOutOfRange
                    ? LocalizationManager.Instance["dialog_goto_errorDmsComponents"]
                    : LocalizationManager.Instance[axis.IsLatitude ? "dialog_goto_errorLatitudeRange" : "dialog_goto_errorLongitudeRange"];

                return false;
            }

            latitude = Latitude.Value;
            longitude = Longitude.Value;
        }
        else
        {
            if (!CoordinateTextParser.TryParseAngle(LatitudeText, out latitude))
            {
                error = string.Format(LocalizationManager.Instance["dialog_goto_errorNumber"], LocalizationManager.Instance["dialog_goto_latitude"]);
                return false;
            }

            if (!CoordinateTextParser.TryParseAngle(LongitudeText, out longitude))
            {
                error = string.Format(LocalizationManager.Instance["dialog_goto_errorNumber"], LocalizationManager.Instance["dialog_goto_longitude"]);
                return false;
            }
        }

        if (!CoordinateTextParser.IsValidLatitude(latitude))
        {
            error = LocalizationManager.Instance["dialog_goto_errorLatitudeRange"];
            return false;
        }

        if (!CoordinateTextParser.IsValidLongitude(longitude))
        {
            error = LocalizationManager.Instance["dialog_goto_errorLongitudeRange"];
            return false;
        }

        wgs84 = ToWgs84(new Point(longitude, latitude), Datum);

        return IsOnEarth(wgs84, out error);
    }

    private bool TryResolveUtm(out Point wgs84, out string? error, ref string? warning)
    {
        wgs84 = null!;

        if (!TryParseField(EastingText, "dialog_goto_easting", out var easting, out error))
            return false;

        if (!TryParseField(NorthingText, "dialog_goto_northing", out var northing, out error))
            return false;

        if (northing < 0 || northing > UtmSouthFalseNorthing)
        {
            error = LocalizationManager.Instance["dialog_goto_errorNorthingRange"];
            return false;
        }

        if (easting < 100_000 || easting > 900_000)
            warning = LocalizationManager.Instance["dialog_goto_warningEastingRange"];

        var utm = UTM.CreateForZone(Datum, UtmZone);

        var local = utm.ToGeodetic(new Point(easting, IsNorthernHemisphere ? northing : northing - UtmSouthFalseNorthing));

        wgs84 = ToWgs84(local, Datum);

        return IsOnEarth(wgs84, out error);
    }

    private bool TryResolveProjected(out Point wgs84, out string? error)
    {
        wgs84 = null!;

        if (!TryParseField(XText, "dialog_goto_x", out var x, out error))
            return false;

        if (!TryParseField(YText, "dialog_goto_y", out var y, out error))
            return false;

        if (!TryGetProjectionParameters(out var parameters, out error))
            return false;

        var ellipsoid = Datum;

        var srs = SelectedProjection.CreateSrs(ellipsoid, parameters);

        var local = srs.ToGeodetic(new Point(x, y));

        // web mercator's ToGeodetic already answers on WGS 84
        wgs84 = SelectedProjection.Kind == ProjectionKind.WebMercator ? local : ToWgs84(local, ellipsoid);

        return IsOnEarth(wgs84, out error);
    }

    private bool IsOnEarth(Point point, out string? error)
    {
        error = null;

        if (double.IsNaN(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.X) || double.IsInfinity(point.Y)
            || !CoordinateTextParser.IsValidLatitude(point.Y) || !CoordinateTextParser.IsValidLongitude(point.X))
        {
            error = LocalizationManager.Instance["dialog_goto_errorProjection"];
            return false;
        }

        return true;
    }

    private static bool TryParseField(string text, string labelKey, out double value, out string? error)
    {
        error = null;

        if (CoordinateTextParser.TryParseNumber(text, out value))
            return true;

        error = string.Format(LocalizationManager.Instance["dialog_goto_errorNumber"], LocalizationManager.Instance[labelKey]);
        return false;
    }

    #endregion

    #region Datum and field synchronisation

    private static Point ToWgs84(Point geodeticOnEllipsoid, Ellipsoid ellipsoid)
    {
        return ellipsoid.AreTheSame(IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84)
            ? geodeticOnEllipsoid
            : Transformations.ChangeDatum(geodeticOnEllipsoid, ellipsoid, IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84);
    }

    private static Point FromWgs84(Point wgs84, Ellipsoid ellipsoid)
    {
        return ellipsoid.AreTheSame(IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84)
            ? wgs84
            : Transformations.ChangeDatum(wgs84, IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84, ellipsoid);
    }

    /// <summary>
    /// Re-expresses a WGS 84 position in the fields of one mode. Used when the map hands the
    /// dialog its centre, when the user switches system, and when a different projection is
    /// picked. Field setters are muted meanwhile so the dialog does not resolve half-filled
    /// input.
    /// </summary>
    private void FillFieldsFrom(Point wgs84, GoToMode mode)
    {
        _isSyncing = true;

        try
        {
            switch (mode)
            {
                case GoToMode.Geodetic:
                {
                    var local = FromWgs84(wgs84, _userDatum);

                    Latitude.Value = local.Y;
                    Longitude.Value = local.X;
                    LatitudeText = FormatDegrees(local.Y);
                    LongitudeText = FormatDegrees(local.X);
                    break;
                }

                case GoToMode.Utm:
                {
                    var local = FromWgs84(wgs84, _userDatum);

                    var zone = MapProjects.FindUtmZone(wgs84.X);
                    var isNorth = wgs84.Y >= 0;

                    var utm = UTM.CreateForZone(_userDatum, zone).FromGeodetic(local);

                    UtmZone = zone;
                    IsNorthernHemisphere = isNorth;
                    EastingText = FormatMetres(utm.X);
                    NorthingText = FormatMetres(isNorth ? utm.Y : utm.Y + UtmSouthFalseNorthing);
                    break;
                }

                case GoToMode.Projected:
                {
                    if (!TryGetProjectionParameters(out var parameters, out _))
                        break;

                    // priming happens for every mode whatever the picker shows, so the datum
                    // is resolved for the projection directly rather than through Datum
                    var ellipsoid = SelectedProjection.AllowsEllipsoid ? _userDatum : SelectedProjection.DefaultEllipsoid;

                    var srs = SelectedProjection.CreateSrs(ellipsoid, parameters);

                    var local = SelectedProjection.Kind == ProjectionKind.WebMercator ? wgs84 : FromWgs84(wgs84, ellipsoid);

                    var xy = srs.FromGeodetic(local);

                    XText = FormatMetres(xy.X);
                    YText = FormatMetres(xy.Y);
                    break;
                }
            }
        }
        catch (Exception)
        {
            // an unrepresentable position (e.g. a pole in Mercator) simply leaves the fields as they were
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Called by the host with the map centre when the dialog opens: every mode is primed with
    /// it, so whichever system the user picks starts from where the map is.
    /// </summary>
    public void SetWebMercatorPoint(Point webMercatorPoint)
    {
        var wgs84 = MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint);

        SetWgs84Point(wgs84);
    }

    public void SetWgs84Point(Point wgs84)
    {
        FillFieldsFrom(wgs84, GoToMode.Geodetic);
        FillFieldsFrom(wgs84, GoToMode.Utm);
        FillFieldsFrom(wgs84, GoToMode.Projected);

        Recalculate();
    }

    #endregion

    #region Formatting

    private static string FormatDegrees(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string FormatMetres(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatUtm(Point wgs84)
    {
        var zone = MapProjects.FindUtmZone(wgs84.X);
        var isNorth = wgs84.Y >= 0;

        var utm = MapProjects.GeodeticToUTM(wgs84, IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoids.WGS84, zone, isNorth);

        return FormattableString.Invariant($"UTM {zone}{(isNorth ? "N" : "S")}   {utm.X:#,##0.0} E   {utm.Y:#,##0.0} N");
    }

    #endregion

    #region Commands

    private RelayCommand? _zoomToCommand;
    public RelayCommand ZoomToCommand => _zoomToCommand ??= new RelayCommand(
        _ => { if (_wgs84Point is not null) _requestZoomTo?.Invoke(_wgs84Point); },
        _ => IsValid);

    private RelayCommand? _panToCommand;
    public RelayCommand PanToCommand => _panToCommand ??= new RelayCommand(
        _ => { if (_wgs84Point is not null) _requestPanTo?.Invoke(_wgs84Point); },
        _ => IsValid);

    private RelayCommand? _addToDrawingCommand;
    public RelayCommand AddToDrawingCommand => _addToDrawingCommand ??= new RelayCommand(
        _ => { if (_wgs84Point is not null) _requestAddToDrawingItem?.Invoke(_wgs84Point); },
        _ => IsValid);

    private RelayCommand? _copyCommand;
    /// <summary>Puts the WGS 84 "lat, lon" reading on the clipboard.</summary>
    public RelayCommand CopyCommand => _copyCommand ??= new RelayCommand(
        _ =>
        {
            if (!IsValid)
                return;

            try
            {
                Clipboard.SetText(ResultDecimalText);
            }
            catch (Exception)
            {
                // the clipboard is occasionally locked by another process; not worth a dialog
            }
        },
        _ => IsValid);

    #endregion
}
