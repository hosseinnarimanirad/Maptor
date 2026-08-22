using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Security;

public class EncryptedMessage
{
    [JsonPropertyName("k")]
    public string IV { get; set; }

    [JsonPropertyName("m")]
    public string Token { get; set; }

    [JsonPropertyName("f")]
    public string Message { get; set; }

    public EncryptedMessage()
    {

    }

    public static EncryptedMessage Create(object value, string base64PubKey)
    {
        //base64Token is a public key in base64 string format

        try
        {
            EncryptedMessage result = new EncryptedMessage();

            RijndaelManaged rm = new RijndaelManaged();

            var encryptedMessage = RijndaelHelper.AesEncrypt(JsonSerializer.Serialize(value), rm.Key, rm.IV);
             
            result.Token = CryptoRSAHelper.RsaEncrypt(Convert.ToBase64String(rm.Key), base64PubKey);

            result.IV = Convert.ToBase64String(rm.IV);

            result.Message = encryptedMessage;

            return result;
        }
        catch (Exception ex)
        {
            return null;
        }

    }

    public T? Decrypt<T>(string base64PriKey) where T : class
    {
        try
        {
            byte[] key = Convert.FromBase64String(CryptoRSAHelper.RsaDecrypt(Token, base64PriKey));

            byte[] iv = Convert.FromBase64String(IV);

            var decryptedParameters = RijndaelHelper.AesDecrypt(Message, key, iv);

            return JsonSerializer.Deserialize<T>(decryptedParameters);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
