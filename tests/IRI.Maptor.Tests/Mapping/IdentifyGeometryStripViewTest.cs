using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Controls.Identify;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models.Identify;
using IRI.Maptor.Presentation.Wpf.ViewModels.Identify;
using IRI.Maptor.Tests.Common;

using Xunit;

using Point = IRI.Maptor.Core.Common.Primitives.Point;
using Size = System.Windows.Size;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The geometry strip of the identify view builds (its StaticResources resolve) and shows only the facts that
/// apply: area, perimeter and points for a one-ring polygon; nothing at all for a point.
/// </summary>
[Collection(WpfCollection.Name)]
public class IdentifyGeometryStripViewTest
{
    private static void ClearSharedResourceDictionaryCache()
    {
        var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as System.Collections.IDictionary;

        lock (cache!)
            cache.Clear();
    }

    /// <summary>
    /// Runs <paramref name="body"/> with a SharedResourceDictionary cache of its own: emptied first (styles another test's
    /// finished thread owns cannot be used here) and again afterwards, so a later view test that does not clear it
    /// (IdentifyResultsDrawingsTest) does not get this thread's styles.
    /// </summary>
    private static void RunWithOwnStyles(System.Action body)
    {
        WpfTestHost.Run(() =>
        {
            ClearSharedResourceDictionaryCache();

            try
            {
                WpfTestHost.EnsureMahAppsResources();

                body();
            }
            finally
            {
                ClearSharedResourceDictionaryCache();
            }
        });
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T match)
                yield return match;

            foreach (var nested in Descendants<T>(child))
                yield return nested;
        }
    }

    private static IdentifyResultsView BuildView(params Feature<Point>[] hits)
    {
        var layer = DrawingItemLayer.Create("parcels", Geometry<Point>.Create(10, 20, SridHelper.WebMercator).AsFeature(), [])!;

        var viewModel = new IdentifyResultsViewModel();

        // Update selects the first feature
        viewModel.Update(new Point(10, 20), [(layer, FeatureSet<Point>.Create("parcels", hits))]);

        var view = new IdentifyResultsView { DataContext = viewModel };

        view.Measure(new Size(900, 600));
        view.Arrange(new Rect(0, 0, 900, 600));
        view.UpdateLayout();

        return view;
    }

    /// <summary>The value of every tile that is not collapsed, in strip order.</summary>
    private static List<string> ShownValues(IdentifyResultsView view) =>
        Descendants<HeaderedContentControl>(Strip(view))
            .Where(t => t.Visibility == Visibility.Visible)
            .Select(t => (string)t.Content)
            .ToList();

    private static Feature<Point> TehranSquare()
    {
        // a 0.01° square near Tehran, as a map layer holds it (Web Mercator); rings are never closed
        var ring = new List<Point> { new(51.40, 35.70), new(51.41, 35.70), new(51.41, 35.71), new(51.40, 35.71) };

        return Geometry<Point>.Create(ring, GeometryType.Polygon, SridHelper.GeodeticWGS84)
                              .Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator)
                              .AsFeature();
    }

    private static Border Strip(IdentifyResultsView view) => (Border)view.FindName("geometryStrip");

    [Fact]
    public void Polygon_ShowsAreaPerimeterAndPoints()
    {
        RunWithOwnStyles(() =>
        {
            var view = BuildView(TehranSquare());

            Assert.Equal(Visibility.Visible, Strip(view).Visibility);

            var values = ShownValues(view);

            // area ≈ 1113 m × 903 m, perimeter ≈ 4.03 km, 4 points; no parts row (one polygon), no holes row
            Assert.Equal(3, values.Count);
            Assert.EndsWith("km\xB2", values[0]);
            Assert.EndsWith("km", values[1]);
            Assert.Equal("4", values[2]);
        });
    }

    [Fact]
    public void Persian_UsesPersianDigits_AndFollowsALanguageSwitch()
    {
        RunWithOwnStyles(() =>
        {
            var localization = LocalizationManager.Instance;
            var previous = localization.CurrentCulture;

            try
            {
                localization.SetCulture(CultureInfo.GetCultureInfo("fa-IR"));

                var view = BuildView(TehranSquare());

                var values = ShownValues(view);

                Assert.Equal("۴", values[2]);
                Assert.DoesNotContain(values, v => v.Any(char.IsAsciiDigit));

                // back to English while the window stays open: LanguageChanged makes the view model
                // re-raise the selection, so the converter runs again
                localization.SetCulture(CultureInfo.GetCultureInfo("en-US"));
                view.UpdateLayout();

                Assert.Equal("4", ShownValues(view)[2]);

                // the measures are rebuilt too, not only the counts
                Assert.Contains(ShownValues(view)[0], c => char.IsAsciiDigit(c));
            }
            finally
            {
                localization.SetCulture(previous);
            }
        });
    }

    [Fact]
    public void UnitCombo_ConvertsLengthAndArea_AndKeepsTheChoiceForTheNextFeature()
    {
        RunWithOwnStyles(() =>
        {
            var second = TehranSquare();
            second.Id = 2;

            var view = BuildView(TehranSquare(), second);
            var viewModel = (IdentifyResultsViewModel)view.DataContext;
            var combo = (ComboBox)view.FindName("measureUnitCombo");

            Assert.Equal(Visibility.Visible, combo.Visibility);
            Assert.Equal(MeasureDisplayUnit.Auto, combo.SelectedValue);

            // picking a unit in the combo: area in its square, perimeter in the unit itself
            combo.SelectedValue = MeasureDisplayUnit.Foot;
            view.UpdateLayout();

            Assert.Equal(MeasureDisplayUnit.Foot, viewModel.MeasureUnit);
            Assert.EndsWith(" ft\xB2", ShownValues(view)[0]);
            Assert.EndsWith(" ft", ShownValues(view)[1]);

            // another feature: the unit stays
            viewModel.SelectedNode = viewModel.Layers[0].Features[1];
            view.UpdateLayout();

            Assert.EndsWith(" ft\xB2", ShownValues(view)[0]);
            Assert.Equal(MeasureDisplayUnit.Foot, combo.SelectedValue);
        });
    }

    [Fact]
    public void Point_HidesTheStrip()
    {
        RunWithOwnStyles(() =>
        {
            var view = BuildView(Geometry<Point>.Create(10, 20, SridHelper.WebMercator).AsFeature());

            Assert.Equal(Visibility.Collapsed, Strip(view).Visibility);
            Assert.False(((IdentifyResultsViewModel)view.DataContext).HasMeasures);
        });
    }

    [Fact]
    public void CountsOnly_HidesTheUnitCombo()
    {
        RunWithOwnStyles(() =>
        {
            var multiPoint = Geometry<Point>.Create([new Point(10, 20), new Point(30, 40)], GeometryType.MultiPoint, SridHelper.WebMercator);

            var view = BuildView(multiPoint.AsFeature());

            Assert.Equal(Visibility.Visible, Strip(view).Visibility);
            Assert.Equal(Visibility.Collapsed, ((ComboBox)view.FindName("measureUnitCombo")).Visibility);
        });
    }
}
