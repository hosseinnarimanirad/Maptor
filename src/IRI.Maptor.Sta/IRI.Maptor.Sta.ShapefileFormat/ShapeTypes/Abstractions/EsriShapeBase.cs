// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.KmlFormat.Primitives;


namespace IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;


public abstract class EsriShapeBase
{

    public abstract BoundingBox MinimumBoundingBox { get; } 


    public int Srid { get; set; }

    public abstract byte[] WriteContentsToByte();

    /// <summary>
    /// Length of the record contents section measured in 16-bit words.
    /// </summary>
    public abstract int ContentLength { get; }

    public abstract EsriShapeType EsriType { get; }

    public abstract bool IsRingBase();

    public abstract bool IsNullOrEmpty();

    public string AsSqlServerWkt() => AsGeometry().AsSqlServerWkt();

    public byte[]? AsWkb() => AsGeometry().AsWkb();

    public abstract PlacemarkType AsPlacemark(Func<Point, Point> projectToGeodeticFunc = null, byte[] color = null);

    public virtual string AsKml(Func<Point, Point> projectToGeodeticFunc = null)
    {
        return OgcKmlMapFunctions.AsKml(this.AsPlacemark(projectToGeodeticFunc));
    }

    public abstract EsriShapeBase Transform(Func<IPoint, IPoint> transform, int newSrid);

    public abstract Geometry<Point> AsGeometry();
}
