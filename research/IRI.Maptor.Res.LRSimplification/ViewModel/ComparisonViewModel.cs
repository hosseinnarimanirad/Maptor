using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Res.LRSimplification.Common;

using static IRI.Maptor.Res.LRSimplification.Common.SingleFeatureVaryingThresholdExperiment;

namespace IRI.Maptor.Res.LRSimplification.ViewModel;

/// <summary>One row of the comparison table: a method, its result and the visual measure against the original.</summary>
public class ComparisonRowViewModel : Notifier
{
    public string Method { get; init; } = string.Empty;
    public string Colour { get; init; } = "#000000";
    public int Points { get; init; }
    public double Compression { get; init; }
    public double ExcessDisplacement { get; init; }
    public double Omitted { get; init; }

    public string CompressionText => $"{Compression:P0}";
    public string ExcessText => ExcessDisplacement == 0 ? "0" : ExcessDisplacement.ToString("G3");
    public string OmittedText => $"{Omitted:P0}";

    /// <summary>The verdict of the measure of the paper: E = 0 is visually equivalent; a part is missing when more than 2 % of the original lies more than 2 px from the result.</summary>
    public string Verdict => Omitted > 0.02 ? "part missing" : ExcessDisplacement == 0 ? "equivalent" : "minor change";
    public string VerdictColour => Omitted > 0.02 ? "#C0392B" : ExcessDisplacement == 0 ? "#4F6F2A" : "#595959";

    internal DrawingItemLayer? Layer { get; set; }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            RaisePropertyChanged();
            if (Layer is not null)
                Layer.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
    }
}

/// <summary>
/// The "Compare simplifications" panel (2026-10-08): open one feature, choose the level of detail, run LRS (adopted traversal)
/// and RDP, BOPW, NOPW, CTR, RW at a 1-px tolerance (CTR added 2026-10-17), and see the compression, the excess displacement E and the omitted share of
/// every result, computed exactly as in the experiments of the paper (exact metric canvas, 2-px margin).
/// </summary>
public class ComparisonViewModel : Notifier
{
    private readonly ApplicationPresenter _presenter;

    // the colours of the paper: original red, the five baselines as in the charts, LRS dark
    private static readonly (string Name, string Colour)[] Methods =
        [("LRS", "#262626"), ("RDP", "#2A78D6"), ("BOPW", "#EB6834"), ("NOPW", "#1BAF7A"), ("CTR", "#C2185B"), ("RW", "#4A3AA7")];

    public const string OriginalColour = "#E8231E";

    public ComparisonViewModel(ApplicationPresenter presenter) => _presenter = presenter;

    public ObservableCollection<ComparisonRowViewModel> Rows { get; } = [];

    public IEnumerable<int> Levels => Enumerable.Range(1, 20);

    private Geometry<Point>? _feature;

    private string _featureName = "no feature loaded";
    public string FeatureName { get => _featureName; set { _featureName = value; RaisePropertyChanged(); } }

    private string _featureInfo = string.Empty;
    public string FeatureInfo { get => _featureInfo; set { _featureInfo = value; RaisePropertyChanged(); } }

    private int _level = 11;
    public int Level { get => _level; set { _level = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ScaleText)); } }

    /// <summary>Approximate map scale of the level at 96 dpi at the feature's latitude.</summary>
    public string ScaleText
    {
        get
        {
            var latitude = _feature is null ? 0 : MapProjects.WebMercatorToGeodeticWgs84(_feature.GetBoundingBox().Center).Y;
            return $"≈ 1:{1.0 / WebMercatorUtility.CalculateMapScale(Level, latitude):N0}";
        }
    }

    private string _status = "Open a GeoJSON feature to start.";
    public string Status { get => _status; set { _status = value; RaisePropertyChanged(); } }

    private RelayCommand? _openCommand;
    public RelayCommand OpenCommand => _openCommand ??= new RelayCommand(async _ =>
    {
        var file = await _presenter.DialogService.ShowOpenFileDialogAsync<MainWindow>("GeoJSON|*.json;*.geojson");
        if (!string.IsNullOrWhiteSpace(file))
            await LoadAsync(file);
    });

    private RelayCommand? _fitLevelCommand;
    public RelayCommand FitLevelCommand => _fitLevelCommand ??= new RelayCommand(_ =>
    {
        if (_feature is not null)
            Level = WebMercatorUtility.EstimateZoomLevel(_feature.GetBoundingBox(), 256, 256);
    });

    private RelayCommand? _runCommand;
    public RelayCommand RunCommand => _runCommand ??= new RelayCommand(async _ => await RunAsync());

    /// <summary>Loads the first feature of a GeoJSON file (WGS 84), as the experiments do.</summary>
    public async Task LoadAsync(string file)
    {
        var feature = GeoJson.LoadFromFile(file).First().Geometry!.TransformToWeMercator();
        feature.Srid = SridHelper.WebMercator;
        feature.RemoveConsecutiveDuplicatePoints();

        _feature = feature;
        FeatureName = Path.GetFileNameWithoutExtension(file);
        FeatureInfo = $"{feature.Type}, {feature.TotalNumberOfPoints} points";
        Level = WebMercatorUtility.EstimateZoomLevel(feature.GetBoundingBox(), 256, 256);
        Rows.Clear();
        _presenter.RemoveAllDrawingItems();
        _presenter.AddDrawingItem(feature, $"{FeatureName} (original)", VisualParameters.GetStroke(OriginalColour, 4));
        var box = feature.GetBoundingBox();
        var margin = Math.Max(box.Width, box.Height) * 0.08;
        _presenter.ZoomToExtent(new BoundingBox(box.XMin - margin, box.YMin - margin, box.XMax + margin, box.YMax + margin), false, true);
        Status = "Choose the level of detail and run the comparison.";
        await Task.CompletedTask;
    }

    /// <summary>Simplifies the feature at <see cref="Level"/> with every method and measures the result against the original.</summary>
    /// <summary>Shows only the given methods on the map (the others stay in the table, unticked).</summary>
    public void ShowOnly(params string[] methods)
    {
        foreach (var row in Rows)
            row.IsVisible = methods.Contains(row.Method);
    }

    public async Task RunAsync()
    {
        if (_feature is null)
        {
            Status = "Open a GeoJSON feature first.";
            return;
        }

        _presenter.IsBusy = true;
        try
        {
            foreach (var row in Rows)
                if (row.Layer is not null) _presenter.RemoveDrawingItem(row.Layer);
            Rows.Clear();

            var feature = _feature;
            var pixel = WebMercatorUtility.ToWebMercatorLength(Level, 1);
            var (extent, size) = MetricCanvas(feature.GetBoundingBox(), pixel, ResubmissionExperiments.MetricMarginPixels);
            using var original = SimplificationHelper.RenderForMetric([feature], extent, size);
            var toScreen = Utility.CreateMapToScreenMapFunc(extent, size.Width, size.Height);
            var lrs = ResubmissionExperiments.ResubmissionLrs();

            foreach (var (name, colour) in Methods)
            {
                Func<Geometry<Point>, Geometry<Point>> simplify = name switch
                {
                    "LRS" => g => g.Simplify(lrs, toScreen, LRHelper.Retain3Points),
                    "RDP" => g => g.Simplify(SimplificationType.RamerDouglasPeucker, CreateParameters(pixel)),
                    "BOPW" => g => g.Simplify(SimplificationType.BeforeOpeningWindow, CreateParameters(pixel)),
                    "NOPW" => g => g.Simplify(SimplificationType.NormalOpeningWindow, CreateParameters(pixel)),
                    "CTR" => g => g.Simplify(SimplificationType.CumulativeTriangleRoutine, CreateParameters(pixel)),
                    _ => g => g.Simplify(SimplificationType.ReumannWitkam, CreateParameters(pixel)),
                };

                var (accuracy, simplified) = await SimplificationHelper.CompareBySingleFeature(feature, simplify, extent, size, original,
                    FeatureName, name, Level, new Stopwatch(), false, measureCollapsed: true);

                var row = new ComparisonRowViewModel
                {
                    Method = name,
                    Colour = colour,
                    Points = simplified.TotalNumberOfPoints,
                    Compression = accuracy.Compression,
                    ExcessDisplacement = accuracy.Raster.ExcessDisplacement,
                    Omitted = accuracy.Raster.OmissionBeyond2,
                };

                _presenter.AddDrawingItem(simplified, $"{FeatureName} {name}", VisualParameters.GetStroke(colour, 2));
                row.Layer = _presenter.DrawingItems.FirstOrDefault();
                Rows.Add(row);
            }

            Status = $"LoD {Level}: LRS without a threshold, baselines at 1 px. E = 0: visually equivalent.";
        }
        catch (Exception ex)
        {
            Status = $"Comparison failed: {ex.Message}";
        }
        finally
        {
            _presenter.IsBusy = false;
        }
    }
}
