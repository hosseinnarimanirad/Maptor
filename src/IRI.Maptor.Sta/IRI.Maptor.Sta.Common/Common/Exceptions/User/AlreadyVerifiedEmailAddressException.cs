using IRI.Maptor.Sta.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class AlreadyVerifiedEmailAddressException : DomainException
{
    public string EmailAddress { get; set; }

    public AlreadyVerifiedEmailAddressException(string emailAddress)
    {
        EmailAddress = emailAddress;
    }

    public override string ResourceKey => "app_sabaApi_error_userEmailAlreadyVerified";

    public override object[] ResourceParameters => [EmailAddress];

    public override ExceptionType ApiExceptionResultType => ExceptionType.Conflict;
}
