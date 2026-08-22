using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Exceptions;

public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string? technicalMessage) : base(technicalMessage)
    {
    }

    public DomainException(string? technicalMessage, Exception? innerException) : base(technicalMessage, innerException)
    {
    }

    public virtual string MessageResourceKey => "message_error_domainException";

    public virtual object[]? MessageResourceParameters => null;

    public virtual ExceptionType ApiExceptionResultType => ExceptionType.BadRequest;
}
