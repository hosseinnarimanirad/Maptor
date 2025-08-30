using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public interface IGeometryAware<T> : IIdentifiable where T : IPoint, new()
{
    Geometry<T> TheGeometry { get; set; }
}
