using System.Net;
using System.Net.Sockets;
using System.Text;

namespace gamething;

// Tiny LAN auto-discovery for the embedded relay. Independent from LiteNetLib —
// uses a plain UDP broadcast on a side port so we don't interfere with the
// game traffic on EmbeddedRelay.Port (9050).
//
// Wire format (UTF-8):
//   client -> "RGTO_DISCOVER"
//   host   -> "RGTO_HOST|<hostName>"
public static class LanDiscovery
{
    public const int DiscoveryPort = 9051;
    private const string DiscoverMsg = "RGTO_DISCOVER";
    private const string HostPrefix = "RGTO_HOST|";

    private static UdpClient? _hostListener;
    private static Thread? _hostThread;
    private static volatile bool _hostRunning;

    public static void StartHost(string hostName)
    {
        StopHost();
        try
        {
            _hostListener = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
            _hostListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _hostListener.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
        }
        catch
        {
            // Port in use or blocked — silently disable LAN discovery on this host.
            _hostListener = null;
            return;
        }

        _hostRunning = true;
        _hostThread = new Thread(() =>
        {
            byte[] reply = Encoding.UTF8.GetBytes(HostPrefix + hostName);
            while (_hostRunning && _hostListener != null)
            {
                try
                {
                    var ep = new IPEndPoint(IPAddress.Any, 0);
                    var data = _hostListener.Receive(ref ep);
                    string msg = Encoding.UTF8.GetString(data);
                    if (msg == DiscoverMsg)
                        _hostListener.Send(reply, reply.Length, ep);
                }
                catch
                {
                    // Socket closed or transient error — exit on shutdown.
                    if (!_hostRunning) break;
                }
            }
        }) { IsBackground = true, Name = "LanDiscoveryHost" };
        _hostThread.Start();
    }

    public static void StopHost()
    {
        _hostRunning = false;
        try { _hostListener?.Close(); } catch { }
        _hostListener = null;
        _hostThread = null;
    }

    /// <summary>
    /// Broadcasts a discovery probe on the LAN and collects host replies for
    /// the given timeout. Returns a list of (ip, hostName) pairs.
    /// </summary>
    public static List<(string ip, string hostName)> FindHosts(int timeoutMs = 800)
    {
        var found = new List<(string, string)>();
        try
        {
            using var udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            udp.Client.ReceiveTimeout = 200;

            byte[] probe = Encoding.UTF8.GetBytes(DiscoverMsg);
            udp.Send(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var ep = new IPEndPoint(IPAddress.Any, 0);
                    var data = udp.Receive(ref ep);
                    string msg = Encoding.UTF8.GetString(data);
                    if (msg.StartsWith(HostPrefix))
                    {
                        string name = msg.Substring(HostPrefix.Length);
                        string ip = ep.Address.ToString();
                        if (!found.Any(f => f.Item1 == ip))
                            found.Add((ip, name));
                    }
                }
                catch (SocketException)
                {
                    // Receive timeout — keep polling until deadline.
                }
            }
        }
        catch
        {
            // Network unavailable or permission denied.
        }
        return found;
    }
}
