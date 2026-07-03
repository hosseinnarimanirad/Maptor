using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Ket.SqlitePersistence.MbTiles;

/// <summary>
/// Describes a single vector layer of a vector MBTiles file, as advertised by the
/// <c>vector_layers</c> entry of the MBTiles <c>json</c> metadata. <see cref="GeometryType"/> is
/// not part of that metadata and is inferred from a sample tile when available.
/// </summary>
public sealed class MvtVectorLayerInfo
{
    public string Id { get; set; } = string.Empty;

    public GeometryType? GeometryType { get; set; }

    public int? MinZoom { get; set; }

    public int? MaxZoom { get; set; }

    public List<Field> Fields { get; set; } = new List<Field>();
}
