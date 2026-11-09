namespace IRI.Maptor.Core.Common.Exceptions;

/// <summary>
/// The file is not one of the formats "add data from file" can open (unknown extension, or JSON that is not
/// GeoJSON, TopoJSON or Esri JSON).
/// </summary>
public class MaptorUnsupportedFileFormatException : DomainException
{
    public static MaptorUnsupportedFileFormatException Instance { get; private set; } = new MaptorUnsupportedFileFormatException();

    public override string MessageResourceKey => "message_error_unsupportedFileFormat";

    public override ExceptionType ApiExceptionResultType => ExceptionType.BadRequest;
}
