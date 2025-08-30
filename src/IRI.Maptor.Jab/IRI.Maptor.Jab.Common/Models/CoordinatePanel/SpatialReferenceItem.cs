using System;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Common.Models.CoordinatePanel;

public class SpatialReferenceItem : Notifier, IDisposable
{
    const string defaultXLabel = "X";
    const string defaultYLabel = "Y";

    private Func<Point, Point> _fromWgs84Geodetic;

    private Func<double, string> _toString;

    public Action<SpatialReferenceItem> FireIsSelectedChanged;

    public SpatialReferenceItem(
        Func<Point, Point> fromWgs84Geodetic,
        Func<double, string> toString,
        string titleItemResourceKey,
        string subTitleItemResourceKey,
        string xLabelResourceKey,
        string yLabelResourceKey,
        string? zoneItemResourceKey = "")
    {
        this._fromWgs84Geodetic = fromWgs84Geodetic;

        this._toString = toString;

        this.TitleItemResourceKey = titleItemResourceKey;

        this.SubTitleItemResourceKey = subTitleItemResourceKey;

        this.XLabelItemResourceKey = xLabelResourceKey;

        this.YLabelItemResourceKey = yLabelResourceKey;

        this.ZoneItemResourceKey = zoneItemResourceKey;

        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RaisePropertyChanged(nameof(TitleItem));
        RaisePropertyChanged(nameof(SubTitleItem));
        RaisePropertyChanged(nameof(ZoneItem));
        RaisePropertyChanged(nameof(XLabelItem));
        RaisePropertyChanged(nameof(YLabelItem));
    }

    public IPoint FromWgs84Geodetic(Point geodeticPoint)
    {
        return _fromWgs84Geodetic(geodeticPoint);
    }

    public void Update(Point geodeticPoint)
    {
        var point = _fromWgs84Geodetic(geodeticPoint);

        this.XValue = _toString(point.X);

        this.YValue = _toString(point.Y);

        this.ZoneNumber = MapProjects.FindUtmZone(geodeticPoint.X).ToString();

        //if (UILanguage == LanguageMode.Persian)
        if (LocalizationManager.Instance.IsPersian)
        {
            this.XValue = this.XValue.LatinNumbersToFarsiNumbers();
            this.YValue = this.YValue.LatinNumbersToFarsiNumbers();
            this.ZoneNumber = this.ZoneNumber.LatinNumbersToFarsiNumbers();
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
                this.FireIsSelectedChanged?.Invoke(this);
            }

        }
    }


    public string GetPositionString(Point geodeticPoint) //where T : IPoint, new()
    {
        var point = FromWgs84Geodetic(geodeticPoint);

        var x = _toString(point.X);

        var y = _toString(point.Y);

        return $"{x},{y}";
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
