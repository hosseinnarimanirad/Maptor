//using System;
//using System.Data;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//using IRI.Maptor.Sta.Common.Model;
//using IRI.Maptor.Sta.Spatial.Model;
//using IRI.Maptor.Sta.Spatial.Helpers;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.SpatialReferenceSystem;
//using IRI.Maptor.Sta.Persistence.Abstractions;

//namespace IRI.Maptor.Sta.Persistence.RasterDataSources;

//public class OnlineGoogleMapDataSource : IRasterDataSource
//{
//    public BoundingBox WebMercatorExtent => BoundingBox.NaN;

//    public int Srid => SridHelper.WebMercator;

//    public OnlineGoogleMapDataSource()
//    {
//        throw new NotImplementedException();
//    }

//    public async Task<GeoReferencedImage> GetTile(BoundingBox mbb, double mapScale)
//    {
//        WiseWebClient client = new WiseWebClient(50);

//        var center = MapProjects.WebMercatorToGeodeticWgs84(mbb.Center);

//        var zoom = WebMercatorUtility.GetZoomLevel(mapScale);

//        var url = $@"https://maps.googleapis.com/maps/api/staticmap?center={center.Y},{center.X}&zoom={zoom}&size=256x256&maptype=roadmap&key=AIzaSyASDX3dnoItXvimcgmsfNgw3J2piODjx9E";

//        var byteImage = await client.DownloadDataTaskAsync(url);

//        return new GeoReferencedImage(byteImage, mbb.Transform(MapProjects.WebMercatorToGeodeticWgs84));
//    }

//    public async Task<Tuple<TileInfo, GeoReferencedImage>> GetTile(TileInfo tile, double mapScale)
//    {
//        try
//        {
//            WiseWebClient client = new WiseWebClient(50);
//            client.Headers.Add("user-agent", "App!");

//            //var zoom = GetZoomLevel(mapScale);
//            var zoom = WebMercatorUtility.GetZoomLevel(mapScale);

//            //google map
//            var url = $@"https://mt0.google.com/vt?x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}";

//            ////google terrain
//            //var url = $@"http://mt1.google.com/vt/lyrs=t@131,r@176163100&hl=en&x={tile.ColumnNumber}&y={tile.RowNumber}&z={tile.ZoomLevel}";

//            var byteImage = await client.DownloadDataTaskAsync(url);

//            System.IO.File.WriteAllBytes($@"C:\Users\Hossein\Desktop\New folder (3)\map{tile.RowNumber}.{tile.ColumnNumber}.{tile.ZoomLevel}.png", byteImage);

//            return new Tuple<TileInfo, GeoReferencedImage>(tile, new GeoReferencedImage(byteImage, tile.WebMercatorExtent.Transform(MapProjects.WebMercatorToGeodeticWgs84)));
//        }
//        catch (Exception)
//        {
//            return null;
//        }

//    }

//    public async Task<List<Tuple<TileInfo, GeoReferencedImage>>> GetTiles(BoundingBox mbb, double mapScale)
//    {
//        var zoom = WebMercatorUtility.GetZoomLevel(mapScale);

//        var tilesBoundary = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(mbb, zoom);

//        //Debug.Print($"#GetTiles: {string.Join(" # ", tilesBoundary.Select(i => i.ToShortString()))}");

//        var result = new List<Tuple<TileInfo, GeoReferencedImage>>();

//        var tasks = new List<Task<Tuple<TileInfo, GeoReferencedImage>>>();

//        foreach (var item in tilesBoundary)
//        {
//            //int width = (int)(item.MercatorExtent.Width * mapScale / unitDistance);

//            //int height = (int)(item.MercatorExtent.Height * mapScale / unitDistance);

//            await Task.Delay(300);

//            //result.Add(await GetTile(width, height, item, mapScale));
//            tasks.Add(GetTile(item, mapScale));
//        }

//        tasks = tasks.Where(i => i != null).ToList();

//        await Task.WhenAll(tasks);

//        foreach (var item in tasks)
//        {
//            result.Add(await item);
//        }

//        return result;
//    }

//}
