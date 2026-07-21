// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;

[Serializable]
public class IrregularDtm
{
    public List<PointM> collection;

    public DelaunayTriangulation triangulation;

    int currentTriangleIndex;

    public int NumberOfPoints
    {
        get { return collection.Count; }
    }

    public IrregularDtm(List<PointM> collection)
    {
        this.collection = collection;

        triangulation = DelaunayTriangulation.Create(GetPoints(collection));

        currentTriangleIndex = 0;
    }

    public IrregularDtm(double[] east, double[] north, double[] value)
    {
        if (east.Length != north.Length || east.Length != value.Length)
        {
            throw new ArgumentException("east, north and value must have the same length.");
        }

        collection = new List<PointM>(east.Length);

        for (int i = 0; i < east.Length; i++)
        {
            collection.Add(new PointM(east[i], north[i], value[i]));
        }

        triangulation = DelaunayTriangulation.Create(GetPoints(collection));

        currentTriangleIndex = 0;
    }

    private static List<Point> GetPoints(List<PointM> collection)
    {
        var result = new List<Point>(collection.Count);

        for (int i = 0; i < collection.Count; i++)
        {
            result.Add(new Point(collection[i].X, collection[i].Y));
        }

        return result;
    }

    public double Interpolate(Point point)
    {
        int triangleIndex = triangulation.FindContainingTriangle(point, currentTriangleIndex);

        if (triangleIndex == -1)
        {
            currentTriangleIndex = 0;

            return double.NaN;
        }

        currentTriangleIndex = triangleIndex;

        var t = triangulation.Triangles[triangleIndex];

        // collection indices align with triangulation.Points indices (points were added in the same order)
        Point firstPoint = triangulation.Points[t.A];
        Point secondPoint = triangulation.Points[t.B];
        Point thirdPoint = triangulation.Points[t.C];

        double firstValue = collection[t.A].M;

        double secondValue = collection[t.B].M;

        double thirdValue = collection[t.C].M;

        double dx1 = secondPoint.X - firstPoint.X;
        double dy1 = secondPoint.Y - firstPoint.Y;
        double dz1 = secondValue - firstValue;
        double dx2 = thirdPoint.X - firstPoint.X;
        double dy2 = thirdPoint.Y - firstPoint.Y;
        double dz2 = thirdValue - firstValue;

        double a = dy1 * dz2 - dy2 * dz1;

        double b = -(dx1 * dz2 - dx2 * dz1);

        double c = dx1 * dy2 - dx2 * dy1;

        double d = a * firstPoint.X + b * firstPoint.Y + c * firstValue;

        return 1 / c * (d - a * point.X - b * point.Y);
    }

    public PointM GetValue(int index)
    {
        return collection[index];
    }

    // when duplicate coordinates exist, the last occurrence wins
    private double GetValue(Point point)
    {
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (collection[i].X == point.X && collection[i].Y == point.Y)
            {
                return collection[i].M;
            }
        }

        throw new ArgumentException("The point is not part of the collection.", nameof(point));
    }

    public double CalculateSlope(Triangle triangle)
    {
        double firstValue = GetValue(triangle.FirstPoint);

        double secondValue = GetValue(triangle.SecondPoint);

        double thirdValue = GetValue(triangle.ThirdPoint);

        double dx1 = triangle.SecondPoint.X - triangle.FirstPoint.X;
        double dy1 = triangle.SecondPoint.Y - triangle.FirstPoint.Y;
        double dz1 = secondValue - firstValue;
        double dx2 = triangle.ThirdPoint.X - triangle.FirstPoint.X;
        double dy2 = triangle.ThirdPoint.Y - triangle.FirstPoint.Y;
        double dz2 = thirdValue - firstValue;

        double a = dy1 * dz2 - dy2 * dz1;

        double b = -(dx1 * dz2 - dx2 * dz1);

        double c = dx1 * dy2 - dx2 * dy1;

        return Math.Sqrt(a * a + b * b) / Math.Sqrt(a * a + b * b + c * c);
    }

    public double CalculateAspect(Triangle triangle)
    {
        double firstValue = GetValue(triangle.FirstPoint);

        double secondValue = GetValue(triangle.SecondPoint);

        double thirdValue = GetValue(triangle.ThirdPoint);

        double dx1 = triangle.SecondPoint.X - triangle.FirstPoint.X;
        double dy1 = triangle.SecondPoint.Y - triangle.FirstPoint.Y;
        double dz1 = secondValue - firstValue;
        double dx2 = triangle.ThirdPoint.X - triangle.FirstPoint.X;
        double dy2 = triangle.ThirdPoint.Y - triangle.FirstPoint.Y;
        double dz2 = thirdValue - firstValue;

        double a = dy1 * dz2 - dy2 * dz1;

        double b = -(dx1 * dz2 - dx2 * dz1);

        double c = dx1 * dy2 - dx2 * dy1;

        if (a == b && a == 0)
            return 0;

        double tempValue = Math.Atan2(b, a);

        return tempValue > 0 ? tempValue : 2 * Math.PI + tempValue;
    }

    public double CalculateVolume(double baseHeight)
    {
        if (triangulation.Triangles.Count < 1)
        {
            throw new InvalidOperationException("The triangulation is empty.");
        }

        double result = 0;

        for (int i = 0; i < triangulation.Triangles.Count; i++)
        {
            var item = triangulation.Triangles[i];

            Triangle temp = triangulation.GetTriangle(i);

            double firstValue = collection[item.A].M;

            double secondValue = collection[item.B].M;

            double thirdValue = collection[item.C].M;

            result += temp.CalculateArea() * (firstValue + secondValue + thirdValue) / 3;
        }

        return result;
    }

    public PointM LowerLeft
    {
        get
        {
            double x = collection[0].X;

            double y = collection[0].Y;

            int index = 0;

            for (int i = 1; i < NumberOfPoints; i++)
            {
                if (collection[i].X < x || collection[i].Y < y)
                {
                    x = collection[i].X;

                    y = collection[i].Y;

                    index = i;
                }
            }

            return new PointM(x, y, collection[index].M);
        }
    }

    public PointM UpperRight
    {
        get
        {
            double x = collection[0].X;

            double y = collection[0].Y;

            int index = 0;

            for (int i = 1; i < NumberOfPoints; i++)
            {
                if (collection[i].X > x || collection[i].Y > y)
                {
                    x = collection[i].X;

                    y = collection[i].Y;

                    index = i;
                }
            }

            return new PointM(x, y, collection[index].M);
        }
    }

    public RegularDtm ToRegularDtm(double cellSize)
    {
        return ToRegularDtm(cellSize, cellSize);
    }

    public RegularDtm ToRegularDtm(double cellWidth, double cellHeight)
    {
        double minX = collection.Min(p => p.X); double maX = collection.Max(p => p.X);

        double minY = collection.Min(p => p.Y); double maxY = collection.Max(p => p.Y);

        int numberOfColumns = (int)Math.Ceiling((maX - minX + 1) / cellWidth);

        int numberOfRows = (int)Math.Ceiling((maxY - minY + 1) / cellHeight);

        double[,] values = new double[numberOfRows, numberOfColumns];

        for (int i = 0; i < numberOfRows; i++)
        {
            for (int j = 0; j < numberOfColumns; j++)
            {
                Point temp = new Point(minX + j * cellWidth, minY + (numberOfRows - 1 - i) * cellHeight);

                values[i, j] = Interpolate(temp);
            }
        }

        return new RegularDtm(values, cellWidth, cellHeight, new Point(minX, minY));
    }



    //public AdjacencyList<Point, double> GetSlopeGraph()
    //{
    //    AdjacencyList<Point, double> result =
    //        new AdjacencyList<Point, double>();//this.triangulation.triangles.Count);

    //    for (int i = 0; i < triangulation.triangles.Count; i++)
    //    {
    //        QuasiTriangle tempQuasiTriangle = triangulation.triangles[i];

    //        int tempCode = tempQuasiTriangle.GetHashCode();

    //        Triangle tempTriangle = triangulation.GeTriangle(tempCode);

    //        Point firstPoint = tempTriangle.CalculateCentroid();

    //        double currentSlope = CalculateSlope(tempTriangle);

    //        foreach (int neighbour in tempQuasiTriangle.OrderedNeighbours)
    //        {
    //            if (neighbour != -1)
    //            {
    //                Triangle neighbourTriangle = triangulation.GeTriangle(neighbour);

    //                Point secondPoint = neighbourTriangle.CalculateCentroid();

    //                double neighbourSlope = CalculateSlope(tempTriangle);

    //                double weight = currentSlope + neighbourSlope;

    //                Connection<Point, double> tempConnection = new Connection<Point, double>(secondPoint, weight);

    //                result.AddUndirectedEdge(firstPoint, secondPoint, weight);
    //            }
    //        }
    //    }

    //    return result;
    //}
}