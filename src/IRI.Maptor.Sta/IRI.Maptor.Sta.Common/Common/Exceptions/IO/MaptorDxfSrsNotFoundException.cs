using IRI.Maptor.Sta.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class MaptorDxfSrsNotFoundException : DomainException
{
    public static MaptorDxfSrsNotFoundException Instance { get; private set; } = new MaptorDxfSrsNotFoundException();

    public override string MessageResourceKey => "message_error_dxfSrsNotFound";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}