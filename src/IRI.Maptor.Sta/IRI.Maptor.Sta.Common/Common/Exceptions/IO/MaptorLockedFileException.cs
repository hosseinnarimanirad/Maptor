using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class MaptorLockedFileException : DomainException
{
    public static readonly MaptorLockedFileException Instance = new MaptorLockedFileException();

    //const string _fileLockedError = "فایل در حال استفاده توسط برنامه دیگری است. لطفا فایل را ببندید و دوباره تلاش کنید.";
    public override string MessageResourceKey => "message_error_lockedFile";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
} 