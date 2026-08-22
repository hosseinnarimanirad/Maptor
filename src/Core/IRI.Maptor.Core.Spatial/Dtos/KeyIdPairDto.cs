using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Spatial.Dtos;

public class KeyIdPairDto
{
    public Guid Key { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int Id { get; set; }
}