using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;

namespace IRI.Maptor.Core.Common.Helpers;

public static class NetworkUtilities
{
    private const string DefaultPingHost = "www.google.com";
    private const string DefaultProbeUri = "https://google.com";

    public static bool PingHost(string nameOrAddress, int timeout = 3000)
    {
        using var pinger = new Ping();
        try
        {
            var reply = pinger.Send(nameOrAddress, timeout);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> PingHostAsync(string hostNameOrAddress, int timeout = 5000)
    {
        using var pingSender = new Ping();
        try
        {
            var reply = await pingSender.SendPingAsync(hostNameOrAddress, timeout);
            return reply?.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsConnectedToInternet(WebProxy? proxy, string? address = null)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return false;
        }

        if (proxy == null)
        {
            var hostReachable = await PingHostAsync(address ?? DefaultPingHost);
            if (hostReachable)
            {
                return true;
            }
        }

        var probeAddress = address ?? DefaultProbeUri;
        var probeResponse = await HttpTransport.GetStringAsync(probeAddress, proxy: proxy);
        return probeResponse.IsSuccess;
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static void SendHtmlMail(string from, string to, string host, string user, string password, string subject, string html, int port = 25, bool enableSsl = false)
    {
        using var mail = new MailMessage(from, to)
        {
            Subject = subject,
            IsBodyHtml = true,
            Body = html
        };

        using var client = new SmtpClient(host, port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, password),
            EnableSsl = enableSsl
        };

        client.Send(mail);
    }

    public static int GetRandomUnusedPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
