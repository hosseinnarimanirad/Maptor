using System;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Persistence.DataSources;

public abstract class RasterDataSource : BaseDataSource, IRasterDataSource
{ 
    public override int Srid => SridHelper.WebMercator;

      
    // Raster data sources are currently read-only and unfiltered,
    // so these flags always remain false for this data source type.
    public override bool HasPendingChanges => false;

    public override bool HasClientFilter => false;
     
     
}
