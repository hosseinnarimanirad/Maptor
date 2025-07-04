﻿using IRI.Extensions;
using IRI.Sta.Spatial.Primitives;
using IRI.Sta.Common.Primitives;
using IRI.Sta.SpatialReferenceSystem;

namespace IRI.Sta.Spatial.MapIndexes;

public class GeodeticSheet : IGeometryAware<Point>
{
    public int Id { get; set; }

    public Geometry<Point> TheGeometry { get => GeodeticExtent.Transform(MapProjects.GeodeticWgs84ToWebMercator).AsGeometry<Point>(SridHelper.WebMercator); set => throw new NotImplementedException(); }

    public BoundingBox GeodeticExtent { get; set; }

    public string SheetName { get; set; }

    //public string SubTitle { get { return SheetName?.Contains(" ") == true ? SheetName.Split(' ')?.LastOrDefault() : string.Empty; } }
    public string SubTitle { get { return SheetName?.Split(' ')?.LastOrDefault() ?? string.Empty; } }

    public string Note { get; set; }

    public GeodeticIndexType Type { get; set; }

    public GeodeticSheet(BoundingBox geodeticExtent, GeodeticIndexType type)
    {
        GeodeticExtent = geodeticExtent;

        Type = type;
    }
}
