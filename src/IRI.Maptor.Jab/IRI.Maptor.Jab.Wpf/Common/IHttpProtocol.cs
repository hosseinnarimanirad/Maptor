using IRI.Maptor.Jab.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Wpf;

public interface IHttpProtocol
{
    void ConfigHttpClient(ProxySettingsModel? model);

    Task<byte[]> GetByteArrayAsync(string? requestUrl);

    Task<byte[]> GetByteArrayAsync(Uri? requestUrl);

    Task<byte[]> GetByteArrayAsync(string? requestUrl, CancellationToken cancellationToken);

    Task<byte[]> GetByteArrayAsync(Uri? requestUrl, CancellationToken cancellationToken);
}
