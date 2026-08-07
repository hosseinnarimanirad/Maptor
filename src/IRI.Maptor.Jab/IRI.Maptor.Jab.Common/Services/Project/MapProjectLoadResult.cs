using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Services.Project;

public class MapProjectLoadResult
{
    public bool Succeeded { get; set; }

    /// <summary>
    /// Display names of the project entries that could not be restored
    /// (missing source files, failed imports, ...).
    /// </summary>
    public List<string> SkippedLayers { get; } = [];
}
