using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Contracts.Mapzen;

public class MapzenRouteCostingModel
{
    public Location[] locations { get; set; }
    public string costing { get; set; }
    public Contour[] contours { get; set; }
    public bool polygons { get; set; }
}

public class Location
{
    public double lat { get; set; }
    public double lon { get; set; }
}

public class Contour
{
    public int time { get; set; }
    public string color { get; set; }
}
