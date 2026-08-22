using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Services;


public static class ResponseFactory
{

    public static Response<T> Create<T>(T result)
    {
        return new Response<T>(result); //{ Result = result, Error = null, IsSuccess = true };
    }

    public static Response<T> CreateError<T>(string errorMessage)
    {
        var error = new ProblemDetails() { Title = errorMessage, Detail = errorMessage };

        return new Response<T>(error); // { Error = error, IsSuccess = false };
    }

    //public static Response<T> CreateCanceled<T>()
    //{
    //    return new Response<T>() { IsCanceled = true };
    //}

    //public static Response<T> CreateCanceled<T>(T result)
    //{
    //    return new Response<T>() { Result = result, IsCanceled = true };
    //}

}
