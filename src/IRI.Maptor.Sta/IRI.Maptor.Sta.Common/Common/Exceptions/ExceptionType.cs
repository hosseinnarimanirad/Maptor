using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

public enum ExceptionType
{
    BadRequest = 1,
    NotFound = 2,
    Unauthorized = 3,
    Conflict = 4,
    InternalServerError = 5,
    Concurrency = 6,
}
