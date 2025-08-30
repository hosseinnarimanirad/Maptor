using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common;

public enum RasterizationMethod            
{
    GdiPlus,
    //OpenTk,
    DrawingVisual,
    WriteableBitmap,
    StreamGeometry,
    None
}
