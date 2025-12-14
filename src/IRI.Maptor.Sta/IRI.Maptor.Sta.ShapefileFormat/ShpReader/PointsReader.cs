// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Common.Primitives;

using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;



namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public abstract class PointsReader<T> : ShpReader<T> where T : EsriShapeBase
{
    public PointsReader(string fileName, EsriShapeType type, int srid)
        : base(fileName, type, srid)
    {

    }

    protected BoundingBox ReadBoundingBox()
    {
        double xMin = shpReader.ReadDouble();

        double yMin = shpReader.ReadDouble();

        double xMax = shpReader.ReadDouble();

        double yMax = shpReader.ReadDouble();

        return new BoundingBox(xMin, yMin, xMax, yMax);
    }

    protected EsriPoint[] ReadPoints(int numberOfPoints, int srid)
    {
        EsriPoint[] result = new EsriPoint[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            double tempX = shpReader.ReadDouble();

            double tempY = shpReader.ReadDouble();

            result[i] = new EsriPoint(tempX, tempY, srid);
        }

        return result;
    }

    //protected EsriPoint[] ReadPoints(int numberOfPoints)
    //{
    //    EsriPoint[] result = new EsriPoint[numberOfPoints];

    //    var byteArray = shpReader.ReadBytes(16 * numberOfPoints);

    //    for (int i = 0; i < numberOfPoints; i++)
    //    {
    //        double tempX = BitConverter.ToDouble(byteArray, i * 16);

    //        double tempY = BitConverter.ToDouble(byteArray, i * 16 + 8);

    //        result[i] = new EsriPoint(tempX, tempY);
    //    }

    //    return result;
    //}

}
