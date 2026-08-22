using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Res.LRSimplification;

public class SpecialFeatureInfo
{
    public Geometry<Point> OriginalGeometry { get; set; }

    public int FeatureIndex { get; set; }

    public int Zoomlevel { get; set; }

    public double diff { get; set; }

    public int Rank { get; set; }

    public List<SimplificationAccuracy> Details { get; set; } = new List<SimplificationAccuracy>(); 
}
 