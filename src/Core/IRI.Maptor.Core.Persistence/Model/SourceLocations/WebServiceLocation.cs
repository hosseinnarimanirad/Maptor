using IRI.Maptor.Core.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Persistence.Model;

/// <summary>
/// A source backed by a web service.
/// </summary>
public sealed class WebServiceLocation : SourceLocation
{
    public string ListUrl { get; set; }

    public string? SyncUrl { get; set; }
}