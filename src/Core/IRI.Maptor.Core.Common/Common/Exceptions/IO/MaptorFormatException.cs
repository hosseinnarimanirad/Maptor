using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Exceptions;

//await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
//await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KMZ یافت نشد.", _error, owner);
//await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KML یافت نشد.", _error, owner);
public class MaptorFormatException : DomainException
{
    public static MaptorFormatException Instance { get; private set; } = new MaptorFormatException();

    public override string MessageResourceKey => "message_error_formatException";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}