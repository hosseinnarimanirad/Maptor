using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// A whole map grid as one layer's worth of features: a polyline per line, and a point per value
/// written against the edges.
/// </summary>
/// <remarks>
/// <para>
/// Built the way <see cref="MgrsGridDataSource"/> is — generated per request from the extent it is
/// asked for, never cached here, no search — with one difference that is the whole point of the
/// feature: the lines are <strong>LineString</strong> features, not polygons. An MGRS square is a
/// named region and is properly a polygon; a grid line is a line.
/// </para>
/// <para>
/// <strong>Lines and values share one source, and therefore one layer.</strong> They are told apart
/// by <see cref="KindFieldName"/>, which every symbolizer filters on, so a grid is a single entry in
/// the legend. Splitting them was tried and reverted: a grid drawn as a group of layers cannot be
/// taken off the map again, because <c>LayerManager.Remove</c> matches its rule only against
/// non-group layers and so never removes the group itself.
/// </para>
/// <para>
/// The definition is held by reference, so a caller that edits its interval, label sides or tier in
/// place gets the change on the next render with nothing to rewire.
/// </para>
/// </remarks>
public class MapGridDataSource : VectorDataSource
{
    /// <summary>
    /// Major, Minor, ZoneSeam or Label — what every symbolizer filters on, so the three line weights
    /// and the values can share one layer.
    /// </summary>
    public const string KindFieldName = "Kind";

    /// <summary>The value of <see cref="KindFieldName"/> on the point features that carry the text.</summary>
    public const string LabelKind = "Label";

    /// <summary>The text itself; the label attribute of a label feature.</summary>
    public const string LabelFieldName = "Label";

    /// <summary>Bottom, Top, Left or Right for a label; empty for a line.</summary>
    public const string SideFieldName = "Side";

    /// <summary>X for a line of constant easting or longitude, Y for constant northing or latitude.</summary>
    public const string AxisFieldName = "Axis";

    /// <summary>The line's own value, in the grid's units: degrees for a graticule, metres otherwise.</summary>
    public const string ValueFieldName = "Value";

    /// <summary>The UTM zone, or 0 for a grid that has no zones.</summary>
    public const string ZoneFieldName = "Zone";

    private static readonly List<Field> _fields = new List<Field>
    {
        new() { IsNullable = false, Name = KindFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = AxisFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = ValueFieldName, Length = 0, TypeFullName = typeof(double).FullName },
        new() { IsNullable = false, Name = ZoneFieldName, Length = 0, TypeFullName = typeof(int).FullName },
        new() { IsNullable = false, Name = LabelFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = SideFieldName, Length = 0, TypeFullName = typeof(string).FullName },
    };

    /// <summary>What kind of grid this is, and how it is labelled. Held by reference, and live.</summary>
    public MapGridDefinition Definition { get; }

    public MapGridOptions Options { get; set; } = MapGridOptions.Default;

    public override string SourceAddress => $"Map Grid Data Source ({Definition.Key})";

    public override BoundingBox WebMercatorExtent
    {
        get => new BoundingBox(-180.0, -MapGridHelper.MaxWebMercatorLatitude, 180.0, MapGridHelper.MaxWebMercatorLatitude)
            .Transform(MapProjects.GeodeticWgs84ToWebMercator);
        protected set => _ = value;
    }

    public override int Srid => SridHelper.WebMercator;

    public override GeometryType? GeometryType
    {
        get => Common.Enums.GeometryType.LineString;
        protected set => _ = value;
    }

    public MapGridDataSource(MapGridDefinition definition) : base(_fields)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public static MapGridDataSource Create(MapGridDefinition definition, MapGridOptions? options = null)
        => new MapGridDataSource(definition) { Options = options ?? MapGridOptions.Default };

    public override string ToString() => $"{nameof(MapGridDataSource)} {Definition}";

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
        => Task.FromResult(Build(boundingBox));

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
        => Task.FromResult(Build(geometry?.GetBoundingBox() ?? WebMercatorExtent));

    /// <summary>A grid has nothing to search: its lines are generated, not stored.</summary>
    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();

    private FeatureSet<Point> Build(BoundingBox webMercatorExtent)
    {
        var grid = MapGridHelper.Create(webMercatorExtent, Definition, Options);

        var features = new List<Feature<Point>>(grid.Lines.Count + grid.Labels.Count);

        foreach (var line in grid.Lines)
        {
            features.Add(new Feature<Point>
            {
                Id = features.Count,

                // No LabelAttribute: nothing is written along a line. The values are the point
                // features below, so they can be placed against the edges of the view.
                TheGeometry = Geometry<Point>.Create(
                    line.WebMercatorPoints, Common.Enums.GeometryType.LineString, SridHelper.WebMercator),

                Attributes = new Dictionary<string, object>
                {
                    { KindFieldName, line.Kind.ToString() },
                    { AxisFieldName, line.Axis.ToString() },
                    { ValueFieldName, line.Value },
                    { ZoneFieldName, line.Zone ?? 0 },
                    { LabelFieldName, string.Empty },
                    { SideFieldName, string.Empty },
                },
            });
        }

        foreach (var label in grid.Labels)
        {
            features.Add(new Feature<Point>
            {
                Id = features.Count,
                LabelAttribute = LabelFieldName,
                TheGeometry = Geometry<Point>.Create(
                    new List<Point> { label.Position }, Common.Enums.GeometryType.Point, SridHelper.WebMercator),
                Attributes = new Dictionary<string, object>
                {
                    { KindFieldName, LabelKind },
                    { AxisFieldName, label.Axis.ToString() },
                    { ValueFieldName, label.Value },
                    { ZoneFieldName, label.Zone ?? 0 },
                    { LabelFieldName, label.Text },
                    { SideFieldName, label.Side.ToString() },
                },
            });
        }

        return FeatureSet<Point>.Create(string.Empty, features);
    }
}
