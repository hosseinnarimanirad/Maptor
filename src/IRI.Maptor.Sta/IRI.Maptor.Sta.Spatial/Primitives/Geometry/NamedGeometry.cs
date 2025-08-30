//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Common.Abstrations;

//namespace IRI.Maptor.Sta.Spatial.Primitives;

//public class NamedGeometry<TPoint> : IGeometryAware<TPoint> where TPoint : IPoint, new()
//{
//    public int Id { get; set; }

//    public Geometry<TPoint> TheGeometry { get; set; }

//    public string Label { get; set; }

//    public NamedGeometry(Geometry<TPoint> geometry, string label)
//    {
//        this.TheGeometry = geometry;

//        this.Label = label;
//    }
//}

//public class NamedGeometry : NamedGeometry<Point>
//{
//    public NamedGeometry(Geometry<Point> geometry, string label) : base(geometry, label)
//    {
//    }
//}