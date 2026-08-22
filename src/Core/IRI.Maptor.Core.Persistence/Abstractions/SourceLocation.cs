using IRI.Maptor.Core.Persistence.Model;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Persistence.Abstractions;

/// <summary>
/// Describes where a data source's data comes from (file, directory, web service, ...)
/// so it can be persisted and the source re-opened later. Serialized polymorphically
/// with a <c>$kind</c> discriminator.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(FileLocation), "file")]
[JsonDerivedType(typeof(DirectoryLocation), "dir")]
[JsonDerivedType(typeof(WebServiceLocation), "web")]
[JsonDerivedType(typeof(GrpcLocation), "grpc")]
public abstract class SourceLocation
{
}








