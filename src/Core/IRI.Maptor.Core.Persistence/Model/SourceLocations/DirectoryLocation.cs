using IRI.Maptor.Core.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Persistence.Model;

/// <summary>
/// A source backed by a directory (e.g. image pyramid, geo-tagged image folder).
/// </summary>
public sealed class DirectoryLocation : SourceLocation
{
    public string Path { get; set; }
}
