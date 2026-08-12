using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Jab.Wpf.ViewModels.Map;
using System;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Jab.Wpf.Layers;

public class GridLayer : SymbolizableLayer
{
    public override LayerType Type => LayerType.VectorLayer;

    //public override BoundingBox Extent { get; protected set; }

    //public override RenderingApproach Rendering { get; protected set; }

    public override string TocGroup
    {
        get => LegendViewModel.NoneTocGroup;
        set => throw new NotImplementedException();
    }

    public GeodeticGridDataSource DataSource { get; set; }

    public GridLayer(GeodeticGridDataSource source)
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
        if (DataSource == null)
            return FeatureSet<Point>.Empty;

        var featureSet = await DataSource.GetAsFeatureSetAsync(mapExtent);

        if (featureSet?.Features == null || featureSet.Features.Count == 0)
            return FeatureSet<Point>.Empty;

        return featureSet;
    }

    #endregion
}
