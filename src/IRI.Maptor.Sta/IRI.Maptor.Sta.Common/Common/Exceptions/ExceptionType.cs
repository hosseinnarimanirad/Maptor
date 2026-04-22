using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Exceptions;

public enum ExceptionType
{
    Unknown = 1,
    BadRequest = 2,
    NotFound = 3,
    Unauthorized = 4,
    Conflict = 5,
    InternalServerError = 6,
    Concurrency = 7,
}
