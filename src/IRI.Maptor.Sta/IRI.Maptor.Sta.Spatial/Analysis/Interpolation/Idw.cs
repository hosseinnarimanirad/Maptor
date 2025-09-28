using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Sta.Spatial.Analysis;

public static class Idw
{
    public static double? Calculate(IEnumerable<Point3D> points, IPoint measurePoint, double? maxDistance)
    {
        //var weights = new List<double>();

        double weightedSum = 0;

        double sum = 0;

        int count = 0;

        foreach (var item in points)
        {
            double distance = SpatialUtility.GetEuclideanDistance(measurePoint, item);

            if (distance < maxDistance)
            {
                double weight = 1.0 / (distance * distance);

                //weights.Add(weight);

                weightedSum += weight * item.Z;

                sum += weight;

                count++;
            }
        }
        if (count > 0)
        {

        }
        return count > 0 ? (double?)(weightedSum / sum) : null;
    }
}
