using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using System.Linq;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Ogc.WMS;

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

    public async Task<List<Geometry<Point>>?> GetLinesAsync(BoundingBox boundingBox)
    {
        if (DataSource is null)
        {
            return null;
        }

        var featureSet = await DataSource.GetAsFeatureSetAsync(boundingBox);
        return featureSet?.Features?.Select(f => f.TheGeometry).ToList();
    }

    #region Overrides

    public override async Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
    {
        if (this.DataSource == null)
            return FeatureSet<Point>.Empty;

        var featureSet = await this.DataSource.GetAsFeatureSetAsync(mapExtent);

        if (featureSet?.Features == null || featureSet.Features.Count == 0)
            return FeatureSet<Point>.Empty;

        return featureSet;
    }

    #endregion
}
