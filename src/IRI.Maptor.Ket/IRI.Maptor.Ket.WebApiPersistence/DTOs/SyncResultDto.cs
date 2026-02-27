using System;

namespace IRI.Maptor.Ket.WebApiPersistence.DTOs;

public class KeyIdPairDto
{
    public Guid Key { get; set; }

    public int Id { get; set; }
}

public class SyncResultDto
{
    public List<KeyIdPairDto> NewIds { get; set; } = new();
}

