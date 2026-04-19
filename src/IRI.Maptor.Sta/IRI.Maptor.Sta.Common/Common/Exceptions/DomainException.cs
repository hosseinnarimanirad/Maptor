using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

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

    public virtual string ResourceKey => "exception_domainException";

    public virtual object[]? ResourceParameters => null;

    public virtual ExceptionType ApiExceptionResultType => ExceptionType.BadRequest;
}
