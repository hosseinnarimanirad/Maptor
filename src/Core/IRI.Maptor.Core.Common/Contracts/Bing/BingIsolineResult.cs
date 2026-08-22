using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Contracts.Bing;

public class BingIsolineResult
{
    public string authenticationResultCode { get; set; }
    public string brandLogoUri { get; set; }
    public string copyright { get; set; }
    public Resourceset[] resourceSets { get; set; }
    public int statusCode { get; set; }
    public string statusDescription { get; set; }
    public string traceId { get; set; }
}

public class Resourceset
{
    public int estimatedTotal { get; set; }
    public Resource[] resources { get; set; }
}

public class Resource
{
    public string __type { get; set; }
    public Polygon[] polygons { get; set; }
}

public class Polygon
{
    public float[][][] coordinates { get; set; }
}
