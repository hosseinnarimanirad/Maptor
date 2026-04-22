using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class MaptorUnknownException : DomainException
{
    public static readonly MaptorUnknownException Instance = new MaptorUnknownException();

    public MaptorUnknownException()
    {
    }

    public MaptorUnknownException(string? technicalMessage) : base(technicalMessage)
    {
    }
     
    public override string MessageResourceKey => "message_error_unknown";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}
 