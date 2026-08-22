using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.Web;

public class SpatialQueryParameters
{
    public string Where { get; set; }

    public string SpatialReference { get; set; }

    public string EsriJsonGeometry { get; set; }

    public bool ReturnIdsOnly { get; set; }

    public string Type { get; set; }
}
