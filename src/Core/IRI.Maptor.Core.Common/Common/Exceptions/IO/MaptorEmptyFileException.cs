using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Exceptions;

//await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل DXF یافت نشد.", _error, owner);
//await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KMZ یافت نشد.", _error, owner);
//await DialogService.ShowMessageAsync("هیچ عارضه‌ای در فایل KML یافت نشد.", _error, owner);
public class MaptorEmptyFileException : DomainException
{
    public static MaptorEmptyFileException Instance { get; private set; } = new MaptorEmptyFileException();

    public override string MessageResourceKey => "message_error_emptyFile";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}