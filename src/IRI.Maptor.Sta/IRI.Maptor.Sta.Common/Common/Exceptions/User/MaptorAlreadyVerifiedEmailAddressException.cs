using IRI.Maptor.Sta.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class MaptorAlreadyVerifiedEmailAddressException : DomainException
{
    public string EmailAddress { get; set; }

    public MaptorAlreadyVerifiedEmailAddressException(string emailAddress)
    {
        EmailAddress = emailAddress;
    }

    public override string MessageResourceKey => "message_error_userEmailAlreadyVerified";

    public override object[] MessageResourceParameters => [EmailAddress];

    public override ExceptionType ApiExceptionResultType => ExceptionType.Conflict;
}
