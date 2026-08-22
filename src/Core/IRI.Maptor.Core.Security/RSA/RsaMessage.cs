using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Security;

public class RsaMessage
{
    //this is in base64 format
    [JsonPropertyName("value")]
    public string Message { get; set; }

    public RsaMessage()
    {

    }


    public static RsaMessage Create(object value, string base64PubKey)
    {
        try
        {
            RsaMessage result = new RsaMessage();

            var message = JsonSerializer.Serialize(value);

            result.Message = CryptoRSAHelper.RsaEncrypt(message, base64PubKey);

            return result;
        }
        catch (Exception ex)
        {
            return null;
        }

    }

    public T Decrypt<T>(string base64PriKey) where T : class
    {
        try
        {
            var decryptedMessage = CryptoRSAHelper.RsaDecrypt(Message, base64PriKey);
              
            return JsonSerializer.Deserialize<T>(decryptedMessage);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

}
