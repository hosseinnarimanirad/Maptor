using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.ShapefileFormat.EsriType;

namespace IRI.Maptor.Extensions;

public static class BoundingBoxExtensions
{
    public static List<EsriPoint> GetClockWiseOrderOfEsriPoints(this BoundingBox boundingBox, int srid)
    {
        return new List<EsriPoint>
        {
            new EsriPoint(boundingBox.XMin, boundingBox.YMin, srid),
            new EsriPoint(boundingBox.XMin, boundingBox.YMax, srid),
            new EsriPoint(boundingBox.XMax, boundingBox.YMax, srid),
            new EsriPoint(boundingBox.XMax, boundingBox.YMin, srid)
        };
    }

    public static EsriPolygon AsEsriShape(this BoundingBox boundingBox, int srid)
    {
        var polygon = boundingBox.GetClockWiseOrderOfEsriPoints(srid);

        polygon.Add(polygon.First()); //first point and last point must be the same

        return new EsriPolygon(polygon.ToArray());
    }

}
