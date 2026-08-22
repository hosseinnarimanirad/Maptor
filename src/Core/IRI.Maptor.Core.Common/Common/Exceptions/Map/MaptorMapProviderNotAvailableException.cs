using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Exceptions;

// MapViewModelBase > SetTileBaseMap
public class MaptorMapProviderNotAvailableException : DomainException
{
    public static MaptorMapProviderNotAvailableException Instance { get; private set; } = new MaptorMapProviderNotAvailableException();

    public override string MessageResourceKey => "message_error_mapProviderNotAvailable";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}
