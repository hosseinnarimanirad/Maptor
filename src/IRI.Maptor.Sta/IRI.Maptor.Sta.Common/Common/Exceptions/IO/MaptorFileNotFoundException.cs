using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

public class MaptorFileNotFoundException : DomainException
{
    //throw new FileNotFoundException($"KML file '{fileName}' was not found.", fileName);
    public string FileName { get; set; }

    public MaptorFileNotFoundException(string fileName)
    {
        FileName = fileName;
    }

    public override string MessageResourceKey => "message_error_fileNotFound";

    public override ExceptionType ApiExceptionResultType => ExceptionType.InternalServerError;
}
