using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.IO.EsriJson;

public enum EsriJsonGeometryType
{
    esriGeometryPoint = 1,
    esriGeometryMultipoint = 2,
    esriGeometryPolyline = 3,
    esriGeometryPolygon = 4,
    esriGeometryEnvelope = 5
}
