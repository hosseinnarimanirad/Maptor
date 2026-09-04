using System;
using System.Globalization;
using System.Windows;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.ViewModels;

/// <summary>
/// The MGRS panel: type a reference at any level, zoom the map to the region it names.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately separate from <see cref="GoToViewModel"/>, which resolves one <em>position</em> and
/// pans to it. An MGRS reference names a <em>square</em>, and the square gets bigger the shorter
/// the reference: <c>39</c> is a whole zone, <c>39S</c> a grid zone cell, <c>39S WV</c> a 100 km
/// square, <c>39S WV 53516 39501</c> a metre. Zooming to an extent is the only sensible answer to
/// all of those, and it is not what the Go To dialog is built to do.
/// </para>
/// <para>
/// Keeping this separate also keeps MGRS out of <c>CoordinateTextParser</c>, which matters:
/// <c>39S 534123 3950123</c> matches its UTM pattern too, because <c>S</c> is both a latitude band
/// letter and a hemisphere letter.
/// </para>
/// </remarks>
public class MgrsGoToViewModel : Notifier
{
    private readonly Action<BoundingBox> _requestZoomTo;

    private BoundingBox _extent = BoundingBox.NaN;

    private string _canonical = string.Empty;

    public MgrsGoToViewModel(Action<BoundingBox> requestZoomTo)
    {
        _requestZoomTo = requestZoomTo;

        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    public static MgrsGoToViewModel Create(MapViewModelBase mapPresenter)
    {
        var model = new MgrsGoToViewModel(geodeticExtent =>
        {
            var webMercatorExtent = geodeticExtent.Transform(MapProjects.GeodeticWgs84ToWebMercator);

            // isExactExtent false so the view keeps its aspect ratio and pads a little: a grid
            // zone cell is far from square, and an exact fit would leave it touching the edges.
            mapPresenter.ZoomToExtent(webMercatorExtent, isExactExtent: false, isNewExtent: true);
        });

        // The map already publishes the cursor's geodetic position on every move; the panel just
        // listens. Kept out of the constructor so the view model stays testable without a map.
        void OnMapChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MapViewModelBase.CurrentPoint))
                model.UpdateCurrentPosition(mapPresenter.CurrentPoint);
        }

        mapPresenter.PropertyChanged += OnMapChanged;

        model.OnDisposing = () => mapPresenter.PropertyChanged -= OnMapChanged;

        model.UpdateCurrentPosition(mapPresenter.CurrentPoint);

        return model;
    }

    /// <summary>Detaches the view model from whatever is feeding it positions.</summary>
    internal Action? OnDisposing { get; set; }

    #region Input

    private string _reference = string.Empty;

    /// <summary>The typed reference, at any level from a bare zone number to a full 1 m square.</summary>
    public string Reference
    {
        get => _reference;
        set
        {
            if (_reference == value)
                return;

            _reference = value;

            RaisePropertyChanged();

            Resolve();
        }
    }

    #endregion

    #region Result

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

            ZoomToCommand.RaiseCanExecuteChanged();
            CopyCommand.RaiseCanExecuteChanged();
        }
    }

    private string _status = string.Empty;

    /// <summary>
    /// One line under the box: the region the reference resolves to, or why it does not resolve.
    /// Empty while the box is empty, so an untouched panel does not read as an error.
    /// </summary>
    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;

            _status = value;

            RaisePropertyChanged();
        }
    }

    private void Resolve()
    {
        if (string.IsNullOrWhiteSpace(Reference))
        {
            _extent = BoundingBox.NaN;
            _canonical = string.Empty;
            IsValid = false;
            Status = string.Empty;

            return;
        }

        if (!MgrsConverter.TryGetBoundingBox(Reference, out var extent))
        {
            _extent = BoundingBox.NaN;
            _canonical = string.Empty;
            IsValid = false;
            Status = LocalizationManager.Instance["dialog_mgrs_invalid"];

            return;
        }

        _extent = extent;
        MgrsConverter.TryNormalize(Reference, out _canonical);
        IsValid = true;
        Status = Describe(extent);
    }

    /// <summary>
    /// The region as "west … east, south … north" in degrees, plus its size. Written with the
    /// invariant culture and Latin digits: a grid reference is read the same way in every UI
    /// language, and so are the degrees beside it.
    /// </summary>
    private static string Describe(BoundingBox extent)
    {
        var box = string.Format(
            CultureInfo.InvariantCulture,
            "{0:F5}° … {1:F5}° E, {2:F5}° … {3:F5}° N",
            extent.XMin, extent.XMax, extent.YMin, extent.YMax);

        var size = string.Format(
            CultureInfo.InvariantCulture,
            "{0:F5}° × {1:F5}°",
            extent.Width, extent.Height);

        return $"{box}  —  {size}";
    }

    private void OnLanguageChanged() => Resolve();

    #endregion

    #region Where the cursor is

    private string _currentPosition = string.Empty;

    /// <summary>
    /// The reference under the mouse pointer, at one metre. Answers the question the grid on its
    /// own cannot: the lines are labelled with principal digits, and this is the whole reference
    /// those digits belong to.
    /// </summary>
    public string CurrentPosition
    {
        get => _currentPosition;
        private set
        {
            if (_currentPosition == value)
                return;

            _currentPosition = value;

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasCurrentPosition));

            CopyCurrentPositionCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasCurrentPosition => !string.IsNullOrEmpty(CurrentPosition);

    /// <summary>
    /// Called on every mouse move over the map. Keeps the last good reading when the pointer
    /// leaves the grid — blanking it as the cursor crosses the poles or the map edge would make
    /// the panel flicker.
    /// </summary>
    public void UpdateCurrentPosition(IRI.Maptor.Core.Common.Primitives.Point? geodeticPoint)
    {
        if (geodeticPoint is null || geodeticPoint.IsNaN())
            return;

        if (MgrsConverter.TryFromGeodetic(geodeticPoint.X, geodeticPoint.Y, MgrsPrecision.M1, out var mgrs))
            CurrentPosition = mgrs;
    }

    #endregion

    #region Commands

    private RelayCommand? _zoomToCommand;

    public RelayCommand ZoomToCommand => _zoomToCommand ??= new RelayCommand(
        _ =>
        {
            if (IsValid && !_extent.IsNaN())
                _requestZoomTo?.Invoke(_extent);
        },
        _ => IsValid);

    private RelayCommand? _copyCommand;

    /// <summary>
    /// Puts the reference on the clipboard in its conventional form — upper case, spaced, leading
    /// zeros kept — rather than however it was typed. Typing <c>39swv5351639501</c> and copying
    /// gives <c>39S WV 53516 39501</c>, which is what other tools expect to be handed.
    /// </summary>
    public RelayCommand CopyCommand => _copyCommand ??= new RelayCommand(
        _ =>
        {
            if (!IsValid || string.IsNullOrEmpty(_canonical))
                return;

            try
            {
                Clipboard.SetText(_canonical);
            }
            catch (Exception)
            {
                // the clipboard is occasionally locked by another process; not worth a dialog
            }
        },
        _ => IsValid);

    private RelayCommand? _copyCurrentPositionCommand;

    /// <summary>Puts the reference under the pointer on the clipboard.</summary>
    public RelayCommand CopyCurrentPositionCommand => _copyCurrentPositionCommand ??= new RelayCommand(
        _ =>
        {
            if (!HasCurrentPosition)
                return;

            try
            {
                Clipboard.SetText(CurrentPosition);
            }
            catch (Exception)
            {
                // the clipboard is occasionally locked by another process; not worth a dialog
            }
        },
        _ => HasCurrentPosition);

    #endregion

    public void Dispose()
    {
        LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged;

        OnDisposing?.Invoke();
        OnDisposing = null;
    }
}
