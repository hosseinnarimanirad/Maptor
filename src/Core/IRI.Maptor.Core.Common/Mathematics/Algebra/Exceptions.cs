using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace IRI.Maptor.Core.Common.Mathematics;

[DataContract]
public class NumberOfElementsException : Exception
{

    public NumberOfElementsException() : base("Incorrect number of elements") { }
    public NumberOfElementsException(string message) : base(message) { }
    public NumberOfElementsException(string message, Exception inner) : base(message, inner) { }
    //protected NumberOfElementsException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}

[DataContract]
public class NonSquareMatrixException : Exception
{

    public NonSquareMatrixException() : base("Matrix must be square") { }
    public NonSquareMatrixException(string message) : base(message) { }
    public NonSquareMatrixException(string message, Exception inner) : base(message, inner) { }
    //protected NonSquareMatrixException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}

[DataContract]
public class OutOfBoundIndexException : Exception
{

    public OutOfBoundIndexException() : base("Index was out of bound") { }
    public OutOfBoundIndexException(string message) : base(message) { }
    public OutOfBoundIndexException(string message, Exception inner) : base(message, inner) { }
    //protected OutOfBoundIndexException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}

[DataContract]
public class IllegalInputException : Exception
{

    public IllegalInputException() : base("Some input values are illegal") { }
    public IllegalInputException(string message) : base(message) { }
    public IllegalInputException(string message, Exception inner) : base(message, inner) { }
    //protected IllegalInputException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}

[DataContract]
public class UnequalMatrixSizeException : Exception
{

    public UnequalMatrixSizeException() : base("Matrices must be the same size") { }
    public UnequalMatrixSizeException(string message) : base(message) { }
    public UnequalMatrixSizeException(string message, Exception inner) : base(message, inner) { }
    //protected UnequalMatrixSizeException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}

[DataContract]
public class ImproperMatrixSizeForMultiplicationException : Exception
{

    public ImproperMatrixSizeForMultiplicationException() : base("Columns of the first Matrix must be equal with rows of second Matrix,in number") { }
    public ImproperMatrixSizeForMultiplicationException(string message) : base(message) { }
    public ImproperMatrixSizeForMultiplicationException(string message, Exception inner) : base(message, inner) { }
    //protected ImproperMatrixSizeForMultiplicationException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}
