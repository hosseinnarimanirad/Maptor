using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Sta.Common.Primitives;  

namespace IRI.Maptor.Jab.Common.Data;

public class MapSettings : IMapSettings
{
    public BoundingBox? InitialExtent { get; set; }
    public bool IsDoubleClickZoomEnabled { get; set; } = true;
    public bool IsGoogleZoomLevelsEnabled { get; set; }
    public bool IsMouseWheelZoomEnabled { get; set; } = true;
    public int MaxGoogleZoomLevel { get; set; } = 22;
    public int MinGoogleZoomLevel { get; set; } = 1;


    public bool CheckIsInScaleRange { get; set; } = true;

    public bool CheckIsVisible { get; set; } = true;


    public static MapSettings Default = new MapSettings();
}
