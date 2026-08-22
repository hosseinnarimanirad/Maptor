using IRI.Maptor.Core.Common.Enums;
using System;
namespace IRI.Maptor.Core.Ogc.SFA;

interface IOgcLineString
{
    WkbByteOrder ByteOrder { get; }
    IOgcPointCollection Points { get; }
    byte[] ToWkb();
    WkbGeometryType Type { get; }
}
