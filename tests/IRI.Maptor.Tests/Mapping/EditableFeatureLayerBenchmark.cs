using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Tests.Common;

using Xunit.Abstractions;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Times of editing a geometry of 100, 1,000 and 10,000 vertices in <see cref="EditableFeatureLayer"/>: starting
/// the edit, moving a vertex, deleting a vertex, inserting one. Synthetic lines; nothing is asserted.
/// <para>
/// It measures the layer only, not the map canvas of MapViewer (no viewer is hosted in tests), and it is not a
/// test: it runs only when the environment variable <c>MAPTOR_BENCHMARK</c> is 1, and writes its table to the test
/// output and, when <c>MAPTOR_BENCHMARK_OUT</c> names a file, to that file.
/// </para>
/// </summary>
[Collection(WpfCollection.Name)]
public class EditableFeatureLayerBenchmark
{
    private readonly ITestOutputHelper _output;

    public EditableFeatureLayerBenchmark(ITestOutputHelper output) => _output = output;

    /// <summary>A zigzag line of <paramref name="count"/> vertices about 10 m apart, in Web Mercator, in Iran.</summary>
    private static Geometry<Point> Line(int count)
    {
        var points = new List<Point>(count);

        for (int i = 0; i < count; i++)
            points.Add(new Point(5_700_000 + i * 10, 4_200_000 + (i % 2) * 5));

        return Geometry<Point>.Create(points, GeometryType.LineString, SridHelper.WebMercator);
    }

    private static readonly string[] MeasureProperties =
    {
        nameof(EditableFeatureLayer.EllipsoidalLength), nameof(EditableFeatureLayer.ScaleFactor),
        nameof(EditableFeatureLayer.EuclideanLengthInUtm), nameof(EditableFeatureLayer.EuclideanLengthInUtm_Refined),
        nameof(EditableFeatureLayer.SphericalLength), nameof(EditableFeatureLayer.GroundLength),
    };

    private static double Milliseconds(Action action)
    {
        var watch = Stopwatch.StartNew();

        action();

        return watch.Elapsed.TotalMilliseconds;
    }

    [Fact]
    public void Measure()
    {
        if (Environment.GetEnvironmentVariable("MAPTOR_BENCHMARK") != "1")
            return;

        var report = new StringBuilder();

        report.AppendLine("vertices | labels | panel bound | start edit ms | one move ms | one delete ms | one insert ms");
        report.AppendLine("---|---|---|---|---|---|---");

        WpfTestHost.Run(() =>
        {
            // warm up: the first marker of each kind pays for parsing its XAML
            Run(20, labels: true, bound: true, moves: 2);

            foreach (var count in new[] { 100, 1_000, 10_000 })
            {
                foreach (var (labels, bound) in new[] { (false, false), (true, false), (true, true) })
                {
                    // a move that rebuilds every label is slow on a big geometry: a few moves are enough to time one
                    var moves = count >= 10_000 ? 5 : count >= 1_000 ? 20 : 100;

                    var (start, move, delete, insert) = Run(count, labels, bound, moves);

                    report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} | {1} | {2} | {3:0.0} | {4:0.000} | {5:0.0} | {6:0.0}",
                        count, labels ? "on" : "off", bound ? "yes" : "no", start, move, delete, insert));
                }
            }
        });

        _output.WriteLine(report.ToString());

        if (Environment.GetEnvironmentVariable("MAPTOR_BENCHMARK_OUT") is { Length: > 0 } file)
            File.WriteAllText(file, report.ToString());
    }

    private static (double Start, double Move, double Delete, double Insert) Run(int count, bool labels, bool bound, int moves)
    {
        var options = EditableFeatureLayerOptions.CreateDefaultForEditing(true, true);

        options.IsMeasureVisible = labels;
        options.IsEdgeLabelVisible = labels;

        EditableFeatureLayer layer = null!;

        var start = Milliseconds(() =>
            layer = new EditableFeatureLayer("edit", Line(count), Transform.Identity, d => d, (_, _) => { }, options));

        using (layer)
        {
            layer.RequestRefresh = _ => { };

            if (bound)
            {
                // what the measure panel's bindings do: read every property that is said to have changed
                layer.PropertyChanged += (_, e) =>
                {
                    if (MeasureProperties.Contains(e.PropertyName))
                        _ = typeof(EditableFeatureLayer).GetProperty(e.PropertyName!)!.GetValue(layer);
                };
            }

            var middle = count / 2;

            var vertex = layer.GetVertices().Items[middle];

            var move = Milliseconds(() =>
            {
                for (int i = 1; i <= moves; i++)
                    vertex.MoveTo(vertex.X + 0.5, vertex.Y + 0.25);
            }) / moves;

            var delete = Milliseconds(() =>
            {
                layer.SelectPoint(middle);
                layer.TryDeleteCurrentPoint();
            });

            // in the middle of the edge after the middle vertex, as a click on the edge handle does
            var edgeStart = layer.GetLastGeometry().Points[middle];
            var edgeEnd = layer.GetLastGeometry().Points[middle + 1];

            var hit = layer.FindEdge(new Point((edgeStart.X + edgeEnd.X) / 2, (edgeStart.Y + edgeEnd.Y) / 2), reach: 1, vertexReach: 0.01)!.Value;

            var insert = Milliseconds(() => layer.InsertVertex(hit, hit.Point));

            return (start, move, delete, insert);
        }
    }
}
