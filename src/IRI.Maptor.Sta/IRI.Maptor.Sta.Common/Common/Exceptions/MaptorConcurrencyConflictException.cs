using IRI.Maptor.Sta.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message) : base(message) { }
    public ConcurrencyConflictException(string message, Exception inner) : base(message, inner) { }

    public override string MessageResourceKey => "message_error_concurrencyConflict";

    public override ExceptionType ApiExceptionResultType => ExceptionType.Concurrency;
}
