using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Presentation.Core.Models;

public class SpatialReferenceItem : Notifier, IDisposable
{
    private CopyCoordinateOptions _copyCoordinateOptions;

    private CoordinateDisplayMode _coordinateDisplayMode;
    public CoordinateDisplayMode CoordinateDisplayMode
    {
        get { return _coordinateDisplayMode; }
        set
        {
            _coordinateDisplayMode = value;
            RaisePropertyChanged();
        }
    }


    public Action<SpatialReferenceItem> FireIsSelectedChanged;

    public SpatialReferenceItem(
        CoordinateDisplayMode coordinateDisplayMode,
        string titleItemResourceKey,
        string subTitleItemResourceKey,
        string xLabelResourceKey,
        string yLabelResourceKey,
        string? zoneItemResourceKey = "")
    {
        CoordinateDisplayMode = coordinateDisplayMode;

        TitleItemResourceKey = titleItemResourceKey;

        SubTitleItemResourceKey = subTitleItemResourceKey;

        XLabelItemResourceKey = xLabelResourceKey;

        YLabelItemResourceKey = yLabelResourceKey;

        ZoneItemResourceKey = zoneItemResourceKey;

        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;

        _copyCoordinateOptions = new CopyCoordinateOptions()
        {
            UseThousandSeparator = true,
            LatLongPrecision = 6,
            XyPrecision = 3
        };
    }

    private void OnLanguageChanged()
    {
        RaisePropertyChanged(nameof(TitleItem));
        RaisePropertyChanged(nameof(SubTitleItem));
        RaisePropertyChanged(nameof(ZoneItem));
        RaisePropertyChanged(nameof(XLabelItem));
        RaisePropertyChanged(nameof(YLabelItem));
    }

    public void Update(Point geodeticPoint)
    {
        var format = CoordinateHelper.Format(MapProjects.GeodeticWgs84ToWebMercator(geodeticPoint), CoordinateDisplayMode, _copyCoordinateOptions);

        XValue = format.x;
        YValue = format.y;

        ZoneNumber = MapProjects.FindUtmZone(geodeticPoint.X).ToString();

        if (LocalizationManager.Instance.IsPersian)
        {
            XValue = XValue.LatinNumbersToFarsiNumbers();
            YValue = YValue.LatinNumbersToFarsiNumbers();
            ZoneNumber = ZoneNumber.LatinNumbersToFarsiNumbers();
        }
    }


    private string TitleItemResourceKey { get; set; }
    public string TitleItem => LocalizationManager.Instance[TitleItemResourceKey];


    private string SubTitleItemResourceKey { get; set; }
    public string SubTitleItem => LocalizationManager.Instance[SubTitleItemResourceKey];


    private string? ZoneItemResourceKey { get; set; }
    public string ZoneItem => string.IsNullOrWhiteSpace(ZoneItemResourceKey) ? string.Empty : LocalizationManager.Instance[ZoneItemResourceKey];


    private string XLabelItemResourceKey { get; set; }
    public string XLabelItem => LocalizationManager.Instance[XLabelItemResourceKey];


    private string YLabelItemResourceKey { get; set; }
    public string YLabelItem => LocalizationManager.Instance[YLabelItemResourceKey];


    private bool _isZoneVisible = false;
    public bool IsZoneVisible
    {
        get { return _isZoneVisible; }
        set
        {
            _isZoneVisible = value;
            RaisePropertyChanged();
        }
    }


    private string _zoneNumber;
    public string ZoneNumber
    {
        get { return _zoneNumber; }
        set
        {
            _zoneNumber = value;
            RaisePropertyChanged();
        }
    }


    private string _xValue;
    public string XValue
    {
        get { return _xValue; }
        set
        {
            _xValue = value;
            RaisePropertyChanged();
        }
    }


    private string _yValue;
    public string YValue
    {
        get { return _yValue; }
        set
        {
            _yValue = value;
            RaisePropertyChanged();
        }
    }


    private bool _isVisible = true;
    public bool IsVisible
    {
        get { return _isVisible; }
        set
        {
            _isVisible = value;
            RaisePropertyChanged();
        }
    }


    private bool _isSelected;
    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            RaisePropertyChanged();

            if (value)
            {
                FireIsSelectedChanged?.Invoke(this);
            }

        }
    }



    #region IDispose

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged;
            }

            // Dispose unmanaged resources here if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
