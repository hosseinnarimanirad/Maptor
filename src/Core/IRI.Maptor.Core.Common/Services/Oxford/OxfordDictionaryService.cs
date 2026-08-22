using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Services;

public static class OxfordDictionaryService
{
    const string baseUrl = "https://od-api.oxforddictionaries.com/api/v1/";
     
    public static async Task<OdResult?> GetExamples(string word, string appId, string appKey)
    {
        try
        {
            using (WebClient client = new WebClient())
            {

                client.Headers.Add(HttpRequestHeader.Accept, "application/json");
                client.Headers.Add("app_id", appId);
                client.Headers.Add("app_key", appKey);
                client.Encoding = Encoding.UTF8;

                var result = await client.DownloadStringTaskAsync($"https://od-api.oxforddictionaries.com/api/v1/entries/en/{word.ToLower()}");

                if (string.IsNullOrWhiteSpace(result))
                {
                    return null;
                }
                else
                {
                    return JsonSerializer.Deserialize<OdResult>(result);
                }
            }
        }
        catch (WebException)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}
