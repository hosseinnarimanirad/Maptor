namespace IRI.Maptor.Ket.WebApiPersistence.DTOs;

/// <summary>
/// Unit-of-work DTO for the update endpoint: added, updated, and deleted features in one request.
/// </summary>
public class FeatureSetChangesDto
{
    public List<FeatureDto> Added { get; set; } = new List<FeatureDto>();

    public List<FeatureDto> Updated { get; set; } = new List<FeatureDto>();

    public List<int> DeletedIds { get; set; } = new List<int>();
}
