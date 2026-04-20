using IRI.Maptor.Sta.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class InvalidPasswordException : DomainException
{
    public override string MessageResourceKey => "app_sabaApi_error_invalidPassword";

    public override ExceptionType ApiExceptionResultType => ExceptionType.BadRequest;
}
