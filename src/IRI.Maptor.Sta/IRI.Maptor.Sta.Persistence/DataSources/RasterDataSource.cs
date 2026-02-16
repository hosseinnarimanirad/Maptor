using System;
using System.Threading.Tasks;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class RasterDataSource : BaseDataSource, IRasterDataSource
{ 
    public override int Srid => SridHelper.WebMercator;

      
    // Raster data sources are currently read-only and unfiltered,
    // so these flags always remain false for this data source type.
    public override bool HasPendingChanges => false;

    public override bool HasClientFilter => false;
     
     
}
