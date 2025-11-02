using IRI.Maptor.Sta.Spatial.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.TileServices;

public abstract class TileServiceUrlStrategy
{
    public abstract string? GetUrl(TileInfo tile);
}
