using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using System.Linq;

namespace IRI.Maptor.Jab.Common;

public class GridLayer : SymbolizableLayer
{
    public override LayerType Type => LayerType.VectorLayer;

    //public override BoundingBox Extent { get; protected set; }

    //public override RenderingApproach Rendering { get; protected set; }

    public GridDataSource DataSource { get; set; }

    public GridLayer(GridDataSource source)
    {
        DataSource = source;
    }

    public List<Geometry<Point>>? GetLines(BoundingBox boundingBox)
    {
        if (DataSource is null)
        {
            return null;
        }

        return DataSource.GetAsFeatureSet(boundingBox).Features.Select(f => f.TheGeometry).ToList(); ;
    }
}
