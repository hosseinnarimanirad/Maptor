using System.IO;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Common.Enums;


namespace IRI.Maptor.Res.TrajectoryCompression;

public static class AreaStatisticsHelper
{

    private static bool retain3Points = false;

    //کد برای تولید فایل‌های مساحت مثلث‌های تشکیل دهنده
    //و خلاصه آمار مربوط به آن
    //این روی تمام شیپ فایل‌های یک فولدر اجرا می‌شود.
    //و خروجی را در قالب دو فایل تکس کنار شیپ فایل 
    //ذخیره می‌کند.
    public static void ProcessShapefilesAndWriteAreas()
    {
        //var fileName = @"E:\University.Ph.D\Manuscripts\Assets\DataWebMercator\OsmRoadWm.shp";

        var groundLevels = Enumerable.Range(0, 19).Select(g => WebMercatorUtility.CalculateGroundResolution(g, 35));

        var folderName = @"E:\University.Ph.D\Manuscripts\Assets\OSM";

        var shpFiles = System.IO.Directory.GetFiles(folderName, "*.shp");

        foreach (var shpFile in shpFiles)
        {
            var features = IRI.Maptor.Sta.ShapefileFormat.Shapefile.ReadShapes(shpFile).Select(g => g.AsGeometry().Project(SrsBases.WebMercator)).ToList();

            AreaStatistics stat = new AreaStatistics(features);

            var totalNumberOfPoints = features.Sum(f => f.TotalNumberOfPoints);

            List<string> header = new List<string>()
                {
                    $"NumberOfFeatures: {features.Count}, TotalNumberOfPoints: {totalNumberOfPoints}, TotalNumberOfTrianlges: {stat.Areas.Count}",
                    GeneralStatistics.CalculateSummary(stat.Areas.ToArray()).ToString()
                };

            File.WriteAllLines($"{shpFile}-summary.txt", header);
            File.WriteAllLines($"{shpFile}-areas.txt", stat.Areas.Select(a => a.ToString()));
        }

    }

    public static void PrintAreas(List<Geometry<Point>> originalGeometry, string outputDirectory, string name, int minLevel, int maxLevel, SimplificationType method)
    {
        AreaStatistics stat = new AreaStatistics(originalGeometry);

        File.WriteAllLines($"{outputDirectory}\\{name}-areas.txt", stat.Areas.Select(a => a.ToString()));

        for (int i = minLevel; i <= maxLevel; i++)
        {
            var groundLevel = WebMercatorUtility.CalculateGroundResolution(i, 35);

            var threshold = groundLevel * groundLevel / 4.0;

            SimplificationParamters parameters = new SimplificationParamters()
            {
                AreaThreshold = threshold,
                Retain3Points = retain3Points
            };

            var simplified = originalGeometry.Select(f => f.Simplify(method, parameters)).Where(f => f.Type != GeometryType.Point).ToList();

            stat = new AreaStatistics(simplified);

            File.WriteAllLines($"{outputDirectory}\\{name}-{i}-{method}-areas.txt", stat.Areas.Select(a => a.ToString()));
        }
    }
}
