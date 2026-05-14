using IRI.Maptor.Sta.Common.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using System.Text;

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

            HttpResponseMessage? result;

            if (parameters.Data is null)
            {
                // Send empty JSON object
                var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");

                result = await client.PostAsync(parameters.Address, emptyContent);
            }
            else
            {
                result = await client.PostAsJsonAsync(parameters.Address, parameters.Data, JsonHelper.IgnoreNullValue);
            }
             
            result.EnsureSuccessStatusCode();

            var jsonString = await result.Content.ReadAsStringAsync();

            return ResponseFactory.Create(JsonHelper.Deserialize<T>(jsonString));
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }

    public static async Task<Response<T>> HttpPostAsync<T>(
    HttpClient client,
    string address) where T : class
    {
        try
        {
            var result = await client.PostAsync(address, null); // No content

            result.EnsureSuccessStatusCode();

            var jsonString = await result.Content.ReadAsStringAsync();

            return ResponseFactory.Create(JsonHelper.Deserialize<T>(jsonString));
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }
}