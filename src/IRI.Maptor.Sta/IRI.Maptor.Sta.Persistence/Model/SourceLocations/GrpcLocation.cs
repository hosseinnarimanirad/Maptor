using IRI.Maptor.Sta.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.Model;

/// <summary>
/// A source backed by a gRPC service. Placeholder: no gRPC data source exists yet.
/// </summary>
public sealed class GrpcLocation : SourceLocation
{
    public string Endpoint { get; set; }

    public string? LayerId { get; set; }
}