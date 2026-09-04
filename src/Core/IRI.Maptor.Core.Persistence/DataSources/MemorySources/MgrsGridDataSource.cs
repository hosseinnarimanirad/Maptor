using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// The whole MGRS grid as one layer's worth of features: a polygon per cell, and a point per piece
/// of text written on it.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="GeodeticGridDataSource"/>, which serves one fixed sheet index per instance,
/// this one picks how fine to draw itself from the extent it is asked for. That is the whole point:
/// one layer, and the grid steps from grid zone cells through 100 km down to 10 m squares as the
/// map zooms in. Set <see cref="Level"/> to pin it instead.
/// </para>
/// <para>Generated per request and never cached: an extent maps to its cells cheaply and exactly.</para>
/// <para>
/// <strong>Cells and text share one source, and therefore one layer.</strong> They are told apart by
/// <see cref="KindFieldName"/>, which every symbolizer filters on. The overlay used to be a group of
/// three layers, and that made it impossible to switch off: <c>LayerManager.Remove</c> matches its
/// rule only against non-group layers, so removing a group by identity silently does nothing and the
/// grid stayed drawn while its ribbon toggle unchecked itself.
/// </para>
/// </remarks>
public class MgrsGridDataSource : VectorDataSource
{
    public const string MgrsFieldName = "MGRS";

    public const string LevelFieldName = "Level";

    /// <summary>
    /// <see cref="CellKind"/>, <see cref="SquareIdKind"/> or <see cref="AxisValueKind"/> — what each
    /// symbolizer filters on, so the cells and the two families of text can share one layer.
    /// </summary>
    public const string KindFieldName = "Kind";

    /// <summary>A grid square: the polygon features.</summary>
    public const string CellKind = "Cell";

    /// <summary>The name of a visible square — <c>39S</c>, or <c>39S WV</c>.</summary>
    public const string SquareIdKind = "SquareId";

    /// <summary>A principal digit on a grid line, written against the edge of the view.</summary>
    public const string AxisValueKind = "AxisValue";

    /// <summary>The text itself; the label attribute of a text feature.</summary>
    public const string LabelFieldName = "Label";

    private static readonly List<Field> _fields = new List<Field>
    {
        new() { IsNullable = false, Name = MgrsFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = LevelFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = KindFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = LabelFieldName, Length = 0, TypeFullName = typeof(string).FullName },
    };

    /// <summary>Pin the grid to one step, or leave null to let the visible extent choose.</summary>
    public MgrsGridLevel? Level { get; set; }

    /// <summary>How many cells may cross the view before the grid steps up to a coarser square.</summary>
    public int MaxCellsAcross { get; set; } = 12;

    /// <summary>A hard ceiling on the cells one request may produce.</summary>
    public int MaxCells { get; set; } = 4000;

    public override string SourceAddress => "MGRS Grid Data Source";

    public override BoundingBox WebMercatorExtent
    {
        get => new BoundingBox(-180.0, MgrsMinLatitude, 180.0, MgrsMaxLatitude).Transform(MapProjects.GeodeticWgs84ToWebMercator);
        protected set => _ = value;
    }

    public override int Srid => SridHelper.WebMercator;

    public override GeometryType? GeometryType
    {
        get => Common.Enums.GeometryType.Polygon;
        protected set => _ = value;
    }

    /// <summary>MGRS covers the UTM band only; the polar caps need UPS, which the library has no projection for.</summary>
    private const double MgrsMinLatitude = -80.0;

    private const double MgrsMaxLatitude = 84.0;

    public MgrsGridDataSource() : base(_fields)
    {
    }

    public static MgrsGridDataSource Create(MgrsGridLevel? level = null) => new MgrsGridDataSource { Level = level };

    public override string ToString() => $"{nameof(MgrsGridDataSource)} {(Level?.ToString() ?? "auto")}";

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        return Task.FromResult(Build(boundingBox));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        var boundingBox = geometry?.GetBoundingBox() ?? WebMercatorExtent;

        return Task.FromResult(Build(boundingBox));
    }

    /// <summary>
    /// Looking a reference up by text belongs to the MGRS panel, which resolves it to an extent.
    /// A grid layer has nothing to search.
    /// </summary>
    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();

    private FeatureSet<Point> Build(BoundingBox webMercatorExtent)
    {
        var grid = MgrsGridHelper.Create(webMercatorExtent, Level, MaxCells);

        var level = grid.Level.ToString();

        var features = new List<Feature<Point>>(grid.Cells.Count + grid.Labels.Count);

        foreach (var cell in grid.Cells)
        {
            features.Add(new Feature<Point>
            {
                Id = features.Count,

                // No LabelAttribute: nothing is written inside a cell. The square's name and the
                // line values are the point features below, so they can be placed and styled on
                // their own.
                TheGeometry = cell.Geometry,
                Attributes = new Dictionary<string, object>
                {
                    { MgrsFieldName, cell.Reference },
                    { LevelFieldName, level },
                    { KindFieldName, CellKind },
                    { LabelFieldName, string.Empty },
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
                    { MgrsFieldName, label.Text },
                    { LevelFieldName, level },
                    {
                        KindFieldName,
                        label.Kind == MgrsGridLabelKind.SquareId ? SquareIdKind : AxisValueKind
                    },
                    { LabelFieldName, label.Text },
                },
            });
        }

        return FeatureSet<Point>.Create(string.Empty, features);
    }
}
