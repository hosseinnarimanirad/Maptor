using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Presentation.Wpf;

public enum MapAction
{
    Pan,
    ZoomIn,
    ZoomOut,
    ZoomInRectangle,
    //ZoomOutRectangle,
    DrawPoint,
    DrawPolyline,
    DrawPolygon,
    DrawRectangle,
    DrawText,
    Identify,
    None,
}
