using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.IO;

public class TiffTag
{
    public ushort Type { get; set; }
    public uint Count { get; set; }
    public uint ValueOrOffset { get; set; }

    public override string ToString() => $"Type: {Type}; Count:{Count}; ValueOrOffset:{ValueOrOffset}";
}