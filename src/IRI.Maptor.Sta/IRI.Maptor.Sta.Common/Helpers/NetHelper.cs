using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class NetHelper
{
    const string _defaulHost = "www.google.com";

    const string contentTypeJson = "application/json";

    const string contentTypeXml = "text/xml";

    const string _defaultUri = "https://google.com";

    public const string ContentTypeApplicationJson = contentTypeJson;

    public const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";



    public static bool PingHost(string nameOrAddress, int timeout = 3000)
    {
        bool pingable = false;


        Ping pinger = new Ping();
        try
        {
            PingReply reply = pinger.Send(nameOrAddress, timeout);

            pingable = reply.Status == IPStatus.Success;
        }
        catch (Exception)
        {
            // Discard PingExceptions and return false;
            return false;
        }

        return pingable;
    }

    public static async Task<bool> PingHostAsync(string hostNameOrAddress, int timeout = 5000)
    {
        bool pingable = false;

        using (Ping pingSender = new Ping())
        {

            try
            {
                var reply = await pingSender.SendPingAsync(hostNameOrAddress, timeout);

                pingable = reply?.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return pingable;
    }

    public static async Task<bool> IsConnectedToInternet(WebProxy? proxy, string? address = null)
    {
        var networkAvailable = NetworkInterface.GetIsNetworkAvailable();

        if (!networkAvailable)
            return false;

        if (proxy == null)
        {
            var isConnected = await PingHostAsync(address ?? _defaulHost);

            if (!isConnected)
                return await OpenRead(proxy, address ?? _defaultUri);

            return isConnected;
            //return await OpenRead(proxy, address ?? _defaultUri);
        }
        else
        {
            return await OpenRead(proxy, address ?? _defaultUri);
        }
    }

    private static async Task<bool> OpenRead(WebProxy? proxy, string address)
    {
        try
        {
            using (var client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, "app!");

                if (proxy?.Address != null)
                {
                    client.Proxy = proxy;
                }

                using (var stream = await client.OpenReadTaskAsync(new Uri(address, UriKind.Absolute)))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);

            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }


    public static void SendHtmlMail(string from, string to, string host, string user, string password, string subject, string html, int port = 25, bool enableSsl = false)
    {
        using (MailMessage mail = new MailMessage(from, to))
        {
            using (SmtpClient client = new SmtpClient(host, port))
            {
                //client.Port = port;

                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                client.UseDefaultCredentials = false;

                client.Credentials = new System.Net.NetworkCredential(user, password);

                client.EnableSsl = enableSsl;

                //client.Host = host;

                mail.Subject = subject;

                mail.IsBodyHtml = true;

                mail.Body = html;

                client.Send(mail);
            }
        }
    }

    public static int GetRandomUnusedPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();

        return port;
    }

    private static WebClient CreateWebClient(string contentType, Encoding? encoding, WebProxy? proxy = null, string? bearer = null, Dictionary<string, string>? headers = null)
    {
        WebClient client = new WebClient();

        client.Headers.Add(HttpRequestHeader.ContentType, contentType ?? contentTypeJson);
        client.Headers.Add(HttpRequestHeader.UserAgent, "application!");

        //client.Headers.Add(HttpRequestHeader.Accept, contentType ?? contentTypeJson);

        if (!string.IsNullOrWhiteSpace(bearer))
        {
            client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {bearer}");
        }

        if (headers != null && headers.Any())
        {
            foreach (var header in headers)
            {
                client.Headers.Add(header.Key, header.Value);
            }
        }

        if (encoding != null)
        {
            client.Encoding = encoding;
        }
        else
        {
            client.Encoding = Encoding.UTF8;
        }

        if (proxy?.Address != null)
        {
            client.Proxy = proxy;
        }

        return client;
    }


    private static System.Net.Http.HttpClient CreateHttpClient(string contentType, Encoding encoding, WebProxy? proxy = null, string? bearer = null, Dictionary<string, string>? headers = null)
    {
        System.Net.Http.HttpClient client;

        if (proxy?.Address != null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = proxy;
            handler.UseProxy = true;
            client = new System.Net.Http.HttpClient(handler);
        }
        else
        {
            client = new System.Net.Http.HttpClient();
        }

        client.DefaultRequestHeaders.Add(HttpRequestHeader.ContentType.ToString(), contentType ?? contentTypeJson);
        client.DefaultRequestHeaders.Add(HttpRequestHeader.UserAgent.ToString(), "application!");

        if (!string.IsNullOrWhiteSpace(bearer))
        {
            client.DefaultRequestHeaders.Add(HttpRequestHeader.Authorization.ToString(), $"Bearer {bearer}");
        }

        if (headers != null && headers.Any())
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        if (encoding is null)
            encoding = Encoding.UTF8;

        client.DefaultRequestHeaders.Add(HttpRequestHeader.AcceptEncoding.ToString(), encoding.ToString());

        return client;
    }


    //Http Get
    #region GET

    public static async Task<Response<T>> HttpGetAsync<T>(string address) where T : class
    {
        return await HttpGetAsync<T>(address, Encoding.UTF8, null);
    }

    public static async Task<Response<T>> HttpGetAsync<T>(string address, Encoding? encoding = null, WebProxy? proxy = null, string? bearer = null, string contentType = contentTypeJson) where T : class
    {
        try
        {
            //WebClient client = new WebClient();

            //client.Headers.Add(HttpRequestHeader.ContentType, contentType);
            //client.Headers.Add(HttpRequestHeader.UserAgent, "application!");

            //client.Encoding = encoding;

            //if (proxy?.Address != null)
            //{
            //    client.Proxy = proxy;
            //}
            var client = CreateWebClient(contentType, encoding, proxy, bearer);

            var stringResult = await client.DownloadStringTaskAsync(address);

            return ResponseFactory.Create(JsonHelper.Deserialize<T>(stringResult));
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }

    public static Response<T> HttpGet<T>(string address, Encoding? encoding = null, WebProxy? proxy = null, string? bearer = null, string contentType = contentTypeJson) where T : class
    {
        try
        {
            //WebClient client = new WebClient();

            //client.Headers.Add(HttpRequestHeader.ContentType, contentType);
            //client.Headers.Add(HttpRequestHeader.UserAgent, "application!");

            //client.Encoding = encoding;

            //if (proxy?.Address != null)
            //{
            //    client.Proxy = proxy;
            //}
            var client = CreateWebClient(contentType, encoding, proxy, bearer);

            var stringResult = client.DownloadString(address);

            return ResponseFactory.Create(JsonHelper.Deserialize<T>(stringResult));
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }

    public static Response<byte[]> HttpGetDownloadData(string address, string contentType, WebProxy? proxy = null)
    {
        try
        {
            //WebClient client = new WebClient();

            //client.Headers.Add(HttpRequestHeader.ContentType, contentType);
            //client.Headers.Add(HttpRequestHeader.UserAgent, "application!");

            //if (proxy?.Address != null)
            //{
            //    client.Proxy = proxy;
            //}
            var client = CreateWebClient(contentType, null, proxy, bearer: null);

            var resultByteArray = client.DownloadData(address);

            return ResponseFactory.Create(resultByteArray);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<byte[]>(ex.Message);
        }
    }

    public static Response<string> HttpGetString(string address, string contentType = contentTypeJson, WebProxy? proxy = null)
    {
        try
        {
            //WebClient client = new WebClient();

            //client.Headers.Add(HttpRequestHeader.ContentType, contentType);
            //client.Headers.Add(HttpRequestHeader.UserAgent, "application!");

            //client.Encoding = Encoding.UTF8;

            //if (proxy?.Address != null)
            //{
            //    client.Proxy = proxy;
            //}
            var client = CreateWebClient(contentType, Encoding.UTF8, proxy, bearer: null);

            var stringResult = client.DownloadString(address);

            return ResponseFactory.Create(stringResult);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<string>(ex.Message);
        }
    }

    #endregion

    #region old

    //public static Response<T> HttpGet<T>(string address, string contentType = contentTypeJson) where T : class
    //{
    //    return HttpGet<T>(address, Encoding.UTF8, contentType, null);
    //}

    //public async static Task<Response<TResponse>> EncryptedHttpGetAsync<TResponse>(string url, string decPriKey) where TResponse : class
    //{
    //    try
    //    {
    //        var response = await NetHelper.HttpGetAsync<EncryptedMessage>(url);

    //        if (response.HasNotNullResult())
    //        {
    //            return ResponseFactory.Create<TResponse>(response.Result.Decrypt<TResponse>(decPriKey));
    //        }
    //        else
    //        {
    //            return ResponseFactory.CreateError<TResponse>(response.ErrorMessage);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return ResponseFactory.CreateError<TResponse>(ex.Message);
    //    }
    //}

    //Http Post
    //public static async Task<Response<T>> HttpPostAsync<T>(string address, object data, WebProxy proxy, string contentType = contentTypeJson)
    //{
    //    return await HttpPostAsync<T>(address, data, Encoding.UTF8, proxy, contentType);
    //}

    //public static Response<T> HttpPost<T>(string address, object data, string contentType = contentTypeJson) where T : class
    //{
    //    return HttpPost<T>(address, data, Encoding.UTF8, null, contentType, null);
    //}

    #endregion

    #region POST

    public static async Task<Response<T>> HttpPostAsync<T>(HttpParameters parameters)
    {
        return await UploadStringTaskAsync<T>(parameters, WebRequestMethods.Http.Post);
    }

    public static Response<T> HttpPost<T>(HttpParameters parameters) where T : class
    {
        try
        {
            var result = HttpPostString(parameters);

            return ResponseFactory.Create<T>(JsonHelper.Deserialize<T>(result.Result));
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.GetFullMessage());
        }
    }

    public static Response<string> HttpPostString(HttpParameters parameters)
    {
        return HttpUploadString(parameters, WebRequestMethods.Http.Post);
        //try
        //{
        //    var client = CreateWebClient(contentType, encoding, proxy, bearer, headers);

        //    var stringData = Newtonsoft.Json.JsonConvert.SerializeObject(data, new Newtonsoft.Json.JsonSerializerSettings()
        //    {
        //        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        //    });

        //    var stringResult = client.UploadString(address, stringData);

        //    return ResponseFactory.Create(stringResult);
        //}
        //catch (Exception ex)
        //{
        //    return ResponseFactory.CreateError<string>(ex.Message);
        //}
    }

    #endregion


    //Put
    public static Task<Response<T>> HttpPutAsync<T>(HttpParameters parameters)
    {
        return UploadStringTaskAsync<T>(parameters, WebRequestMethods.Http.Put);
    }

    public static Response<string> HttpPutString(HttpParameters parameters)
    {
        return HttpUploadString(parameters, WebRequestMethods.Http.Put);
    }


    //General
    private static async Task<Response<T>> UploadStringTaskAsync<T>(HttpParameters parameters, string httpMethod)
    {
        try
        {
            var client = CreateWebClient(parameters.ContentType, parameters.Encoding, parameters.Proxy, parameters.Bearer, parameters.Headers);


            var stringData = JsonHelper.SerializeWithIgnoreNullOption(parameters.Data);

            //var stringData = Newtonsoft.Json.JsonConvert.SerializeObject(parameters.Data, new Newtonsoft.Json.JsonSerializerSettings()
            //{
            //    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            //});

            var stringResult = await client.UploadStringTaskAsync(parameters.Address, httpMethod, stringData);

            var result = ResponseFactory.Create(JsonHelper.Deserialize<T>(stringResult));

            return result;
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<T>(ex.Message);
        }
    }

    private static Response<string> HttpUploadString(HttpParameters parameters, string httpMethod)
    {
        try
        {
            var client = CreateWebClient(parameters.ContentType, parameters.Encoding, parameters.Proxy, parameters.Bearer, parameters.Headers);

            var stringData = JsonHelper.SerializeWithIgnoreNullOption(parameters.Data);
            //var stringData = Newtonsoft.Json.JsonConvert.SerializeObject(parameters.Data, new Newtonsoft.Json.JsonSerializerSettings()
            //{
            //    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            //});

            var stringResult = client.UploadString(parameters.Address, httpMethod, stringData);

            return ResponseFactory.Create(stringResult);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<string>(ex.Message);
        }
    }


    // ***** XML ***** 
    public static Response<string> HttpPostXml(string address, string xmlData, Encoding encoding, WebProxy? proxy = null)
    {
        try
        {
            var client = CreateWebClient(contentTypeXml, encoding, proxy, bearer: null);

            var stringResult = client.UploadString(address, xmlData);

            return ResponseFactory.Create<string>(stringResult);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<string>(ex.Message);
        }
    }

    public static async Task<Response<string>> HttpPostXmlAsync(string address, string xmlData, Encoding encoding, WebProxy? proxy = null)
    {
        try
        {
            var client = CreateWebClient(contentTypeXml, encoding, proxy);

            var stringResult = await client.UploadStringTaskAsync(address, xmlData);

            return ResponseFactory.Create<string>(stringResult);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<string>(ex.Message);
        }
    }


    public static Response<byte[]> HttpPostDownloadData(string address, object data, Encoding encoding, string contentType = contentTypeJson, WebProxy? proxy = null, string? bearer = null)
    {
        try
        {
            //WebClient client = new WebClient();

            //client.Headers.Add(HttpRequestHeader.ContentType, contentType);
            //client.Headers.Add(HttpRequestHeader.UserAgent, "application!");

            //if (!string.IsNullOrWhiteSpace(bearer))
            //{
            //    client.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {bearer}");
            //}

            //client.Encoding = encoding;

            //if (proxy?.Address != null)
            //{
            //    client.Proxy = proxy;
            //}
            var client = CreateWebClient(contentType, encoding, proxy, bearer);

            var stringData = JsonHelper.SerializeWithIgnoreNullOption(data);
            //var stringData = Newtonsoft.Json.JsonConvert.SerializeObject(data, new Newtonsoft.Json.JsonSerializerSettings()
            //{
            //    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            //});

            var result = client.UploadData(address, Encoding.UTF8.GetBytes(stringData));

            return ResponseFactory.Create(result);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<byte[]>(ex.Message);
        }

    }


    public static Response<bool> HttpDelete(string address, string contentType = contentTypeJson, WebProxy? proxy = null, string? bearerToken = null)
    {
        try
        {
            var client = CreateWebClient(contentType, Encoding.UTF8, proxy, bearer: bearerToken);

            var stringResult = client.UploadData(address, "DELETE", new byte[0]);

            return ResponseFactory.Create(true);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<bool>(ex.Message);
        }
    }


}

public class HttpParameters
{
    public string Address { get; set; }

    public object? Data { get; set; }

    public Encoding Encoding { get; set; }

    public WebProxy Proxy { get; set; }

    public string Bearer { get; set; }

    public string ContentType { get; set; }

    public Dictionary<string, string> Headers { get; set; }
}

