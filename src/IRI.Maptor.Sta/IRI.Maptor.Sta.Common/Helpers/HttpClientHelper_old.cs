using IRI.Maptor.Sta.Common.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class HttpClientHelper_Old
{
    public static async Task<Response<T>> HttpPostAsync<T>(
        HttpClient client,
        HttpParameters parameters) where T : class
    {
        try
        {
            var json = JsonHelper.SerializeWithIgnoreNullOption(parameters.Data);

            var result = await client.PostAsJsonAsync(parameters.Address, parameters.Data, JsonHelper.IgnoreNullValue);

            result.EnsureSuccessStatusCode();

            var jsonString = await result.Content.ReadAsStringAsync();
            
            return ResponseFactory.Create(JsonHelper.Deserialize<T>(jsonString));
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        } 
    }


    //public static async Task<Response<T>> HttpGetAsync<T>(
    //    HttpClient client,
    //    HttpParameters parameters) where T : class
    //{
    //    try
    //    {
    //        var result = await client.GetFromJsonAsync<T>(parameters.Address,)
    //    }
    //    catch (Exception ex)
    //    {
    //        return ResponseFactory.CreateError<T>(ex.Message);
    //    }
    //}

}

