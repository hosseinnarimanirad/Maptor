using System.Text.Json.Serialization;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.IranRepo;

public class NccPoint : IGeometryAware<Point>
{
    public int Id { get; set; }

    public Guid Key { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonIgnore()]
    public string Type { get; set; }

    [JsonPropertyName("X")]
    public double XWebMercator { get; set; }

    [JsonPropertyName("Y")]
    public double YWebMercator { get; set; }

    public Geometry<Point> _geometry;

    [JsonIgnore]
    public Geometry<Point> TheGeometry
    {
        get
        {
            return Geometry<Point>.Create(XWebMercator, YWebMercator, SridHelper.WebMercator);
        }
        set => throw new NotImplementedException();
    }

    //[JsonIgnore]
    //public SqlGeometry TheSqlGeometry { get => TheGeometry.AsSqlGeometry(); set => throw new NotImplementedException(); }

    //public SqlFeature AsSqlFeature()
    //{
    //    return new SqlFeature()
    //    {
    //        TheSqlGeometry = TheSqlGeometry,
    //        LabelAttribute = nameof(Name),
    //        Attributes = new Dictionary<string, object>()
    //        {
    //            {nameof(this.Id), this.Id },
    //            {nameof(this.Name), this.Name },
    //            {nameof(this.Type), this.Type },
    //            {nameof(this.XWebMercator), this.XWebMercator },
    //            {nameof(this.YWebMercator), this.YWebMercator },
    //        }
    //    };
    //}


    public Feature<Point> AsFeature()
    {
        return new Feature<Point>()
        {
            TheGeometry = TheGeometry,
            LabelAttribute = nameof(Name),
            Attributes = new Dictionary<string, object>()
            {
                {nameof(this.Id), this.Id },
                {nameof(this.Name), this.Name },
                {nameof(this.Type), this.Type },
                {nameof(this.XWebMercator), this.XWebMercator },
                {nameof(this.YWebMercator), this.YWebMercator },
            }
        };
    }
}
