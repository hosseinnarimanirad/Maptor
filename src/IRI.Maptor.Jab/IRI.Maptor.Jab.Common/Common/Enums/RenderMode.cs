using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common;

public enum RenderMode
{
    //Render the entire extent at once
    Default,
    //Tile the extent and render each tile separatedly
    Tiled
}
