using IRI.Maptor.Core.Common.Enums;
using System;

namespace IRI.Maptor.Core.Ogc.SFA;

public interface IOgcGeometry
{
    WkbByteOrder ByteOrder { get; }

    byte[] ToWkb();

    WkbGeometryType Type { get; }
}
