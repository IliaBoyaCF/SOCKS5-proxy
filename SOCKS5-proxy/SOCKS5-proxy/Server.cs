using SOCKS5_proxy.Session;
using System.Net;
using System.Net.Sockets;

namespace SOCKS5_proxy;

public class Server : IDisposable
{
    public static readonly int s_defaultTimeout = 1_000; // microseconds, which is 1 millisecond
    
    private readonly Socket _serverSocket;
    private readonly int _port;
    private readonly List<ISession> _openedSessions = [];
    private readonly Selector _selector;
    private readonly DNSResolver _dnsResolver = DNSResolver.Instance;

    public Server(int port)
    {
        _port = port;
        _serverSocket = InitServerSocket(port);
        _selector = new Selector();
    }

    public void Dispose()
    {
        _serverSocket.Dispose();
        foreach (ISession socket in _openedSessions)
        {
            socket.Dispose();
        }
    }

    public void Start()
    {
        Console.WriteLine("Server started on port {0}, available interfaces to connect:", _port);

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress curAdd in host.AddressList)
        {
            Console.WriteLine(curAdd.ToString());
        }

        _selector.AttachSelectable(_serverSocket, (ISubscibableSelector.ReadableHandler)HandleWrite);
        DNSResolver.Instance.AttachOnSelector(_selector);
        while (true)
        {
            _selector.Select(s_defaultTimeout);
        }
    }

    private static Socket InitServerSocket(int port)
    {
        Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
        socket.Bind(localEndPoint);
        socket.Listen();
        return socket;
    }

    private void OnSessionClose(ISession session)
    {
        session.Dispose();
        _openedSessions.Remove(session);   
    }

    public void HandleWrite(Socket writableSocket)
    {
        Socket socket = _serverSocket.Accept();
        socket.Blocking = false;
        Console.WriteLine("Connected with {0}", socket.RemoteEndPoint);
        ISession session = new SessionImplementation(socket, _selector);
        _openedSessions.Add(session);
        session.SessionClosed += OnSessionClose;
    }
}