using IRI.Maptor.Sta.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.Model;

/// <summary>
/// A source backed by a directory (e.g. image pyramid, geo-tagged image folder).
/// </summary>
public sealed class DirectoryLocation : SourceLocation
{
    public string Path { get; set; }
}
