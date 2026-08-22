using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IRI.Maptor.Core.Common.Exceptions;

[DataContract]
public class MaptorZeroSizeArrayException : Exception
{

    public MaptorZeroSizeArrayException() : base("Array size is zero") { }
    public MaptorZeroSizeArrayException(string message) : base(message) { }
    public MaptorZeroSizeArrayException(string message, Exception inner) : base(message, inner) { }

}
