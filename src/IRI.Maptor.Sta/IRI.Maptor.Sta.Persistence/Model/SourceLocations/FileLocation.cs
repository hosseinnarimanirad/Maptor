using IRI.Maptor.Sta.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.Model;

/// <summary>
/// A source backed by a single file, optionally a sub-dataset inside it
/// (e.g. a GeoPackage/MBTiles/personal-gdb table or layer).
/// </summary>
public sealed class FileLocation : SourceLocation
{
    public string Path { get; set; }

    public string? TableName { get; set; }
}