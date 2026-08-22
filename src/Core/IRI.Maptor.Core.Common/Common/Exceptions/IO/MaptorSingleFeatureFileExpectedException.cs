using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Exceptions;

public class MaptorSingleFeatureFileExpectedException : DomainException
{
    public static readonly MaptorSingleFeatureFileExpectedException Instance = new MaptorSingleFeatureFileExpectedException();

    //const string _fileLockedError = "فایل در حال استفاده توسط برنامه دیگری است. لطفا فایل را ببندید و دوباره تلاش کنید.";
    public override string MessageResourceKey => "message_error_singleFeatureFileExpected";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}