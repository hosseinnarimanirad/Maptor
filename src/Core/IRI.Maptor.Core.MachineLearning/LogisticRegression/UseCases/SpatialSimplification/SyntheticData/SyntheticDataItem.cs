using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.MachineLearning;

public class SyntheticDataItem
{
    // Screen 
    public string OriginalLineString { get; set; }

    // Screen 
    public string SimplifiedLineString { get; set; }

    public string Title { get; set; }

    public string Note { get; set; }
    //public GeoJsonFeature Original { get; set; }


    public static List<SyntheticDataItem> CreatePolar(double radius, int count)
    {
        Point center = new Point(0, 0);

        Point first = new Point(radius, 0);

        var dTheta = Math.PI / count;

        List<Point> points = Enumerable.Range(1, count).Select(i => Point.FromPolar(radius, i * dTheta)).ToList();

        Point last = new Point(-radius, 0);

        if (!points.Contains(last))
        {
            points.Add(last);
        }

        return null;
    }

    public override string ToString()
    {
        return $"Title: {Title}, Note: {Note}";
    }
}
