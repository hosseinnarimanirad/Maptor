using System.Text;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.ShapefileFormat.Model;

namespace IRI.Maptor.Sta.ShapefileFormat;

public static class WebMercatorHelper
{
    public static void WriteGeodeticBoundingBoxToGoogleTileRegionsAsShapefile(string fileName, BoundingBox geodeticBoundingBox, int zoomLevel)
    {
        var tiles = WebMercatorUtility.GeodeticBoundingBoxToGoogleTileRegions(geodeticBoundingBox, zoomLevel);

        Shapefile.Save(
            fileName,
            tiles,
            t => t.GeocentricExtent.AsEsriShape(SridHelper.GeodeticWGS84),
            new List<ObjectToDbfTypeMap<TileInfo>>()
                {
                    new ObjectToDbfTypeMap<TileInfo>(DbfFieldDescriptors.GetIntegerField("Google Zoom"), t => t.ZoomLevel),
                    new ObjectToDbfTypeMap<TileInfo>(DbfFieldDescriptors.GetIntegerField("Row Number"), t => t.RowNumber),
                    new ObjectToDbfTypeMap<TileInfo>(DbfFieldDescriptors.GetIntegerField("Column Number"), t => t.ColumnNumber)
                },
            Encoding.ASCII,
            new NoProjection(),
            true); 
    }

    public static void WriteWebMercatorBoundingBoxToGoogleTileRegionsAsShapefile(string fileName, BoundingBox webMercatorBoundingBox, int zoomLevel)
    {
        var geographicBoundingBox = webMercatorBoundingBox.Transform(i => MapProjects.WebMercatorToGeodeticWgs84(i));

        WriteGeodeticBoundingBoxToGoogleTileRegionsAsShapefile(fileName, geographicBoundingBox, zoomLevel);
    }

}
