using IRI.Maptor.Core.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Exceptions;

public class MaptorUserNotFoundException : DomainException
{
    public override string MessageResourceKey => "message_error_userNotFound";

    public override ExceptionType ApiExceptionResultType => ExceptionType.NotFound;
}
