using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models;

public class IdentifyOptions
{
    // ignore unvisible layers or not in identify
    public bool IncludeInvisibleLayers { get; set; } = true;

    // ignore layers which are not in scale range or not
    public bool IncludeNotInScaleRangeLayers { get; set; } = true;

    // the radius in pixels on screen to select features
    public int SelectionTolerance { get; set; } = 7;
}
