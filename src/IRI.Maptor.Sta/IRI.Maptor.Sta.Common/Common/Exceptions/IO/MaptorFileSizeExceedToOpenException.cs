using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;


public class MaptorFileSizeExceedToOpenException : DomainException
{
    public static MaptorFileSizeExceedToOpenException Instance { get; private set; } = new MaptorFileSizeExceedToOpenException();

    //await DialogService.ShowMessageAsync("حجم فایل انتخابی بیش از حد مجاز است", _error, owner);
    public override string MessageResourceKey => "message_error_fileSizeExceedToOpen";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}
