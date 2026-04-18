using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Services;

public class Response<T>
{
    public bool IsSuccess { get; set; }

    public bool IsCanceled { get; set; }

    public bool IsFailed => !IsSuccess && !IsCanceled;

    public int StatusCode { get; set; }

    public ProblemDetails? Error { get; set; }

    [Obsolete("Use Error?.Detail or Error?.Title")]
    public string? ErrorMessage => Error?.Detail ?? Error?.Title;

    public T Result { get; set; }
     
    public bool HasNotNullResult()
    {
        return !FailedOrCanceled() && Result != null;
    }

    public bool IsNullOrEmpty()
    {
        return FailedOrCanceled() || Result == null;
    }

    public bool FailedOrCanceled()
    {
        return IsCanceled == true || IsFailed == true;
    }
}

public static class ResponseFactory
{
    public static Response<T> Create<T>(T result)
    {
        return new Response<T>() { Result = result, Error = null, IsSuccess = true };
    }

    public static Response<T> CreateError<T>(string errorMessage)
    {
        var error = new ProblemDetails() { Title = errorMessage, Detail = errorMessage };

        return new Response<T> { Error = error, IsSuccess = false };
    }
}
