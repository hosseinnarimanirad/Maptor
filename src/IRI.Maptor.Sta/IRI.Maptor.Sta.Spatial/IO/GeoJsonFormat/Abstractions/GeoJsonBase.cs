using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem; 

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Base class for GeoJSON geometry types providing common functionality.
/// </summary>
public abstract class GeoJsonBase : IGeoJsonGeometry
{
    /// <summary>
    /// Gets or sets the type of the geometry.
    /// </summary>
    public abstract string? Type { get; set; }

    /// <summary>
    /// Gets the geometry type as a GeometryType enum value.
    /// </summary>
    public abstract GeometryType GeometryType { get; }

    public abstract bool HasZ{ get; }

    // standard GeoJson does not supports M,
    // check the readme file for more information
    public abstract bool HasM { get; }

    /// <summary>
    /// Determines whether this geometry is null or empty.
    /// </summary>
    /// <returns>True if the geometry is null or empty; otherwise, false.</returns>
    public abstract bool IsNullOrEmpty();

    /// <summary>
    /// Gets the number of geometries in this GeoJSON geometry.
    /// </summary>
    /// <returns>The number of geometries.</returns>
    public abstract int NumberOfGeometries();

    /// <summary>
    /// Gets the total number of points in this geometry.
    /// </summary>
    /// <returns>The total number of points.</returns>
    public abstract int NumberOfPoints();

    /// <summary>
    /// Parses this GeoJSON geometry to an IGeometry instance.
    /// The returned geometry will use Point, PointZ, or PointZM based on the coordinate dimensions detected.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public abstract IGeometry Parse(bool isLongitudeFirst = true, int srid = 0);

    /// <summary>
    /// Serializes this geometry to a JSON string.
    /// </summary>
    /// <param name="indented">If true, the JSON output will be indented.</param>
    /// <param name="removeSpaces">If true, all spaces will be removed from the output.</param>
    /// <returns>A JSON string representation of this geometry.</returns>
    public string Serialize(bool indented, bool removeSpaces = false)
    {
        return GeoJson.Serialize(this, indented, removeSpaces);
    }

    /// <summary>
    /// Transforms this geometry to Web Mercator projection.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>A Geometry&lt;Point&gt; instance in Web Mercator projection.</returns>
    public Geometry<Point> TransformToWeMercator(bool isLongitudeFirst = true)
    {
        var geometry = this.Parse(isLongitudeFirst, SridHelper.GeodeticWGS84);
        // Convert to Geometry<Point> for transformation
        if (geometry is Geometry<Point> pointGeometry)
        {
            return pointGeometry.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
        }
        // For PointZ and PointZM, convert to Point for transformation
        if (geometry is Geometry<PointZ> pointZGeometry)
        {
            var pointGeom = new Geometry<Point>(pointZGeometry.Points.Select(p => new Point(p.X, p.Y)).ToList(), geometry.Type, geometry.Srid);
            return pointGeom.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
        }
        if (geometry is Geometry<PointZM> pointZMGeometry)
        {
            var pointGeom = new Geometry<Point>(pointZMGeometry.Points.Select(p => new Point(p.X, p.Y)).ToList(), geometry.Type, geometry.Srid);
            return pointGeom.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
        }
        throw new NotSupportedException($"Unsupported geometry type: {geometry.GetType()}");
    }

    /// <summary>
    /// Converts this geometry to a GeoJSON Feature.
    /// </summary>
    /// <returns>A GeoJsonFeature instance.</returns>
    public GeoJsonFeature AsFeature() => GeoJson.AsFeature(this);

    /// <summary>
    /// Converts this geometry to a GeoJSON FeatureSet.
    /// </summary>
    /// <returns>A GeoJsonFeatureSet instance.</returns>
    public GeoJsonFeatureSet AsFeatureSet() => GeoJson.AsFeatureSet(this);

}
