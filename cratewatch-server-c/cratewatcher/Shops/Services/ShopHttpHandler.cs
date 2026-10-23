using System.Net;
using System.Net.Sockets;

namespace CrateWatch.Shops.Services;

// Shop URLs are typed in by a user; never let a scan reach our own network. The check runs on every
// connection, so redirects and DNS tricks cannot get around it.
public static class ShopHttpHandler
{
    public static SocketsHttpHandler Create() => new()
    {
        UseProxy = false,
        MaxAutomaticRedirections = 5,
        ConnectCallback = ConnectToPublicAddressAsync,
    };

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
        var address = addresses.FirstOrDefault(IsPublic)
                      ?? throw new HttpRequestException("The shop address is not a public address.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && (address.GetAddressBytes()[0] & 0xFE) != 0xFC;
        }

        var b = address.GetAddressBytes();
        return !(b[0] is 0 or 10
                 || (b[0] == 100 && b[1] is >= 64 and <= 127)
                 || (b[0] == 169 && b[1] == 254)
                 || (b[0] == 172 && b[1] is >= 16 and <= 31)
                 || (b[0] == 192 && b[1] == 168));
    }
}
