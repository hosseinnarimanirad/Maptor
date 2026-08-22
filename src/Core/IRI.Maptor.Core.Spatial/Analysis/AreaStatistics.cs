using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

public class AreaStatistics 
{
    public AreaStatistics(List<Geometry<Point>> geometries)
    {
        this.NumberOfPoints = geometries.Sum(g => g.TotalNumberOfPoints);

        this.Areas = SpatialUtility.GetPrimitiveAreas(geometries);
    }

    public AreaStatistics(Geometry<Point> geometry)
    {
        this.NumberOfPoints = geometry.TotalNumberOfPoints;

        this.Areas = SpatialUtility.GetPrimitiveAreas(geometry);
    }

    internal int NumberOfPoints;

    public readonly List<double> Areas;

    public string Description
    {
        get { return GetDescription(); }
    }

    internal double GetAverage()
    {
        if (this.NumberOfPoints == 0)
            return 0;

        return Areas.Average();
    }

    public double GetStandardDeviation()
    {
        if (this.NumberOfPoints == 0)
            return 0;

        return IRI.Maptor.Core.Common.Mathematics.Statistics.CalculateStandardDeviation(Areas);
    }

    public override string ToString()
    {
        if (this.NumberOfPoints == 0)
        {
            return "EMPTY STAT";
        }

        return $"NumberOfTriangles: {Areas.Count}, St.Dev.: {GetStandardDeviation()}, AvgArea: {Areas.Average()}, Number of Points: {NumberOfPoints}, MinArea: {Areas.Min()}, MaxArea: {Areas.Max()}";
    }

    public string ToCSV()
    {
        if (this.NumberOfPoints == 0)
        {
            return "EMPTY STAT";
        }

        return $"{Areas.Count}; {GetStandardDeviation()}; {Areas.Average()}; {NumberOfPoints}; {Areas.Min()}; {Areas.Max()}";
    }

    public string GetDescription()
    {
        return this.ToString().Replace(',', '\n');
    }

    public double[] GetHistogram(int categories)
    {
        var areas = this.Areas;

        var ordered = areas.OrderBy(i => i).ToList();

        var size = areas.Count / categories;

        return Enumerable.Range(0, categories).Select(i => ordered[(i) * size]).ToArray();
    }
}
