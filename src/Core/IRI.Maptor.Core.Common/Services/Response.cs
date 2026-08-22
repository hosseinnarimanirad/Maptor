using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Services;

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

    private Response()
    {

    }

    public Response(T result)
    {
        this.Result = result;

        this.IsSuccess = true;

        this.Error = null;
    }

    public Response(ProblemDetails error)
    {
        this.Error = error;

        this.IsSuccess = false;
    }

    public static Response<T> Empty => new Response<T>();

    #region Factory methods

    public static Response<T> Create(T result)
    {
        return new Response<T>() { Result = result, Error = null, IsSuccess = true };
    }

    public static Response<T> Create(bool isSuccess, int statusCode)
    {
        return new Response<T>() { IsSuccess = isSuccess, StatusCode = statusCode };
    }

    //public static Response<T> CreateError(string errorMessage)
    //{
    //    var error = new ProblemDetails() { Title = errorMessage, Detail = errorMessage };

    //    return new Response<T> { Error = error, IsSuccess = false };
    //}

    public static Response<T> CreateFailed()
    {
        return new Response<T>() { IsSuccess = false };
    }

    //public static Response<T> CreateFailed(T result)
    //{
    //    return new Response<T>() { Result = result, IsSuccess = false };
    //}

    public static Response<T> CreateCanceled()
    {
        return new Response<T>() { IsCanceled = true };
    }

    public static Response<T> CreateCanceled(T result)
    {
        return new Response<T>() { Result = result, IsCanceled = true };
    }


    #endregion
}
