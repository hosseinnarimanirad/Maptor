using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Contracts.Here;

public class HereIsolineResult
{
    public MapzenResponse response { get; set; }
}

public class MapzenResponse
{
    public Metainfo metaInfo { get; set; }
    public Center center { get; set; }
    public Isoline[] isoline { get; set; }
    public Start start { get; set; }
}

public class Metainfo
{
    public DateTime timestamp { get; set; }
    public string mapVersion { get; set; }
    public string moduleVersion { get; set; }
    public string interfaceVersion { get; set; }
    public string[] availableMapVersion { get; set; }
}

public class Center
{
    public float latitude { get; set; }
    public float longitude { get; set; }
}

public class Start
{
    public string linkId { get; set; }
    public Mappedposition mappedPosition { get; set; }
    public Originalposition originalPosition { get; set; }
}

public class Mappedposition
{
    public float latitude { get; set; }
    public float longitude { get; set; }
}

public class Originalposition
{
    public float latitude { get; set; }
    public float longitude { get; set; }
}

public class Isoline
{
    public int range { get; set; }
    public Component[] component { get; set; }
}

public class Component
{
    public int id { get; set; }
    public string[] shape { get; set; }
}
