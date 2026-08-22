using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Infrastructure.GdiPlus.Model;

public class GeoTaggedImage
{
    public PointZM GeographicLocation { get; set; }

    public PointZM WebMercatorLocation { get; set; }

    public string ImageFileName { get; set; }

    public GeoTaggedImage(string imageFileName)
    {
        try
        {
            this.ImageFileName = imageFileName;

            var location = System.IO.Path.ChangeExtension(imageFileName, ".corx");

            if (System.IO.File.Exists(location))
            {
                this.GeographicLocation = JsonHelper.Deserialize<PointZM>(System.IO.File.ReadAllText(location));

                var webMercator = MapProjects.GeodeticWgs84ToWebMercator((Point)GeographicLocation);

                this.WebMercatorLocation = new PointZM(webMercator.X, webMercator.Y, GeographicLocation.Z);
            }
            else
            {
                using (var bitmap = new System.Drawing.Bitmap(imageFileName))
                {
                    this.GeographicLocation = Helpers.ImageHelper.GetWgs84Location(bitmap);

                    if (GeographicLocation.IsNaN())
                        return;

                    var webMercator = MapProjects.GeodeticWgs84ToWebMercator((Point)GeographicLocation);

                    this.WebMercatorLocation = new PointZM(webMercator.X, webMercator.Y, GeographicLocation.Z);

                    System.IO.File.WriteAllText(location, JsonHelper.Serialize(this.GeographicLocation));
                }
            }
        }
        catch (Exception)
        {
            this.GeographicLocation = PointZM.NaN;

            this.WebMercatorLocation = PointZM.NaN;
        }
    }
}
