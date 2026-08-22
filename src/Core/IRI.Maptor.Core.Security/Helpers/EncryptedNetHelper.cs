using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Security;
using IRI.Maptor.Core.Common.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Security.Helpers;

public static class EncryptedNetHelper
{

    // 1400.02.14
    // send encrypted message and get encrypted message
    public async static Task<Response<TResponse>> EncryptedHttpPostAsync<TResponse>(string url, object parameter, WebProxy proxy, string encPubKey, string decPriKey) where TResponse : class
    {
        try
        {
            var message = EncryptedMessage.Create(parameter, encPubKey);

            //var response = await NetHelper.HttpPostAsync<EncryptedMessage>(url, message, null, proxy);
            var response = await HttpTransport.PostAsync<EncryptedMessage>(url, message, proxy: proxy);

            if (response.HasNotNullResult())
            {
                return ResponseFactory.Create<TResponse>(response.Result.Decrypt<TResponse>(decPriKey));
            }
            else
            {
                return ResponseFactory.CreateError<TResponse>(response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<TResponse>(ex.Message);
        }
    }


    // 1400.02.14
    // send encrypted message and get simple message
    public async static Task<Response<TResponse>> EncryptedHttpPostAsync<TResponse>(string url, object parameter, WebProxy proxy, string encPubKey) where TResponse : class
    {
        try
        {
            var message = EncryptedMessage.Create(parameter, encPubKey);

            //var response = await NetHelper.HttpPostAsync<EncryptedMessage>(url, message, null, proxy);
            var response = await HttpTransport.PostAsync<TResponse>(url, message, proxy: proxy);

            return response;
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<TResponse>(ex.Message);
        }
    }

}
