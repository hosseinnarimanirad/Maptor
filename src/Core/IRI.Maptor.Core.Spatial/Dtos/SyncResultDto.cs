using IRI.Maptor.Core.Spatial.Dtos;
using System;

namespace IRI.Maptor.Core.Spatial.Dtos;
 
public class SyncResultDto
{
    public List<KeyIdPairDto> NewIds { get; set; } = new();

    public List<KeyIdPairDto> UpdatedRowVersions { get; set; } = new();
}

