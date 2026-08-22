using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using IRI.Maptor.Res.Sfc.View;
using IRI.Maptor.Core.Common.DataStructures;
using IRI.Maptor.Core.Spatial.Analysis.SFC;

namespace IRI.Maptor.Res.Sfc;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private double GetUnitDistance()
    {
        double? _unitDistance = null;

        if (_unitDistance == null || double.IsNaN(_unitDistance.Value))
        {
            PresentationSource source = PresentationSource.FromVisual(this);

            if (source == null)
                return 1;

            double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;

            //size of each pixel (in meter)
            _unitDistance = IRI.Maptor.Core.Common.Helpers.ConversionHelper.InchToMeterFactor / dpiX;
        }

        return _unitDistance.Value;
    }

    private void openKdTree(object sender, RoutedEventArgs e)
    {
        MainWindow window = new MainWindow();

        window.Show();
    }

    private void openKdTreeSfc(object sender, RoutedEventArgs e)
    {
        KdTreeWindow window = new KdTreeWindow();

        window.Show();
    }

    private void openRTreeSfc(object sender, RoutedEventArgs e)
    {
        RTreeWindow window = new RTreeWindow();

        window.Show();
    }

    private void openSfc(object sender, RoutedEventArgs e)
    {
        RasterSfcWindow window = new RasterSfcWindow();

        window.Show();

    }

    private void pointSorting(object sender, RoutedEventArgs e)
    {
        PointDistributionOrderingWindow window = new PointDistributionOrderingWindow();

        window.Show();
    }

    private void BigDataSetButton_Click(object sender, RoutedEventArgs e)
    {

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        int[] numberOfPoints = new int[] { 100, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000, 200000, 500000, 1000000, int.MaxValue };

        var mainPoints = GetRandomArray();

        for (int i = 0; i < numberOfPoints.Length; i++)
        {
            string query = string.Empty;

            //***********************************Earthquake
            //if (numberOfPoints[i] == int.MaxValue)
            //{
            //    query = "SELECT lat,_long FROM Earthquakes";
            //}
            //else
            //{
            //    query = "SELECT top(" + numberOfPoints[i] + ") lat,_long FROM Earthquakes";
            //}

            ////read the points
            //SqlServerDataSource source = new SqlServerDataSource("data source=.;integrated security=true;initial catalog = IRI.Database", "Earthquakes", "Location");
            //var points = source.SelectFeatures(query).Select(d => new IRI.Maptor.Core.Common.Primitives.Point((double)d["lat"], (double)d["_long"])).ToArray();

            //***********************************waze
            //if (numberOfPoints[i] == int.MaxValue)
            //{
            //    query = "SELECT Latitude,Longitude FROM Users";
            //}
            //else
            //{
            //    query = "SELECT top(" + numberOfPoints[i] + ") Latitude,Longitude FROM Users";
            //}

            //SqlServerDataSource source = new SqlServerDataSource("data source=.;integrated security=true;initial catalog = WaseDb", "Users", "Location");
            //var points = source.SelectFeatures(query).Select(d => new IRI.Maptor.Core.Common.Primitives.Point((double)d["Latitude"], (double)d["Longitude"])).ToArray();




            //***********************************random
            var points = mainPoints.Take(numberOfPoints[i] == int.MaxValue ? mainPoints.Length : numberOfPoints[i]).ToArray();

            watch.Restart();

            //order the points
            PointOrdering.HilbertSorter(points);
            //IRI.Maptor.Infrastructure.DataStructure.SortAlgorithm.MergeSort(points, (p1, p2) => p1.X.CompareTo(p2.X));

            watch.Stop();
            Debug.WriteLine($"{points.Count()}, {watch.ElapsedMilliseconds / 1000.0}");
        }

        ////show result
    }

    IRI.Maptor.Core.Common.Primitives.Point[] GetRandomArray()
    {
        Random xRandom = new Random(13);

        Random yRandom = new Random(40);

        IRI.Maptor.Core.Common.Primitives.Point[] result = new IRI.Maptor.Core.Common.Primitives.Point[1500000];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new IRI.Maptor.Core.Common.Primitives.Point(xRandom.Next(0, 100000), yRandom.Next(0, 100000));
        }

        return result;
    }

    private void Test()
    {
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        Random r = new Random();

        int[] array10k = Enumerable.Repeat(0, 10000).Select(i => r.Next()).ToArray();

        SortAlgorithm.Heapsort<int>(array10k, (p1, p2) => p1.CompareTo(p2));

        watch.Stop();
        Debug.WriteLine($"Heapsort, {array10k.Length}, {watch.ElapsedMilliseconds / 1000.0}");
        watch.Restart();

        SortAlgorithm.MergeSort<int>(array10k, (p1, p2) => p1.CompareTo(p2));

        watch.Stop();
        Debug.WriteLine($"MergeSort, {array10k.Length}, {watch.ElapsedMilliseconds / 1000.0}");
        watch.Restart();

        SortAlgorithm.QuickSort<int>(array10k, (p1, p2) => p1.CompareTo(p2));

        watch.Stop();
        Debug.WriteLine($"QuickSort, {array10k.Length}, {watch.ElapsedMilliseconds / 1000.0}");
        watch.Restart();

        SortAlgorithm.BubbleSort<int>(array10k, (p1, p2) => p1.CompareTo(p2));

        watch.Stop();
        Debug.WriteLine($"BubbleSort, {array10k.Length}, {watch.ElapsedMilliseconds / 1000.0}");
        watch.Restart();

    }
}
