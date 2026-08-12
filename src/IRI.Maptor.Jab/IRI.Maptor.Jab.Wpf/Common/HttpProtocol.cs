using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Wpf;

public class HttpProtocol : IHttpProtocol
{
    HttpClient? _httpClient;

    public HttpProtocol(HttpClient? httpClient)
    {
        _httpClient = httpClient;
    }

    public void ConfigHttpClient(ProxySettingsModel? model)
    {
        var proxy = model?.GetProxy();

        var timeout = model?.TimeOutInSeconds ?? 15;

        if (proxy?.Address != null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = proxy;
            handler.UseProxy = true;
            _httpClient = new HttpClient(handler) { Timeout = new TimeSpan(0, 0, seconds: timeout) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "app!");
        }
        else
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = null;
            handler.UseProxy = false;
            _httpClient = new HttpClient(handler) { Timeout = new TimeSpan(0, 0, seconds: timeout) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "app!");
        }
    }

    public async Task<byte[]> GetByteArrayAsync(string? requestUrl)
        => _httpClient is null ? Array.Empty<byte>() : await _httpClient.GetByteArrayAsync(requestUrl);

    public async Task<byte[]> GetByteArrayAsync(Uri? requestUrl)
        => _httpClient is null ? Array.Empty<byte>() : await _httpClient.GetByteArrayAsync(requestUrl);

    public async Task<byte[]> GetByteArrayAsync(string? requestUrl, CancellationToken cancellationToken)
        => _httpClient is null ? Array.Empty<byte>() : await _httpClient.GetByteArrayAsync(requestUrl, cancellationToken);

    public async Task<byte[]> GetByteArrayAsync(Uri? requestUrl, CancellationToken cancellationToken)
        => _httpClient is null ? Array.Empty<byte>() : await _httpClient.GetByteArrayAsync(requestUrl, cancellationToken);
}
