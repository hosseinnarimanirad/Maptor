using IRI.Maptor.Sta.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.Model;

/// <summary>
/// A source backed by a web service.
/// </summary>
public sealed class WebServiceLocation : SourceLocation
{
    public string ListUrl { get; set; }

    public string? SyncUrl { get; set; }
}