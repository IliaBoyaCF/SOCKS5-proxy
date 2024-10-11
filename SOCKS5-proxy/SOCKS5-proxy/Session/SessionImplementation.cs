using System.Net.Sockets;

namespace SOCKS5_proxy.Session;

public class SessionImplementation : ISession
{
    public static readonly int s_bufferSize = 4096;
    public ISession.StateType State { get { return _stateType; } }

    public event ISession.OnSessionClose? SessionClosed;

    private readonly Socket _clientSocket;
    private readonly ISubscibableSelector _selector;
    private readonly DNSResolver _dnsResolver;
    private Socket? _hostSocket;

    private SessionState _state;
    private ISession.StateType _stateType = ISession.StateType.ACTIVE;

    public SessionImplementation(Socket clientSocket, ISubscibableSelector selector, DNSResolver dnsResolver)
    {
        _clientSocket = clientSocket;
        _selector = selector;
        _dnsResolver = dnsResolver;
        _state = new NegotiationState(this, _clientSocket, _selector);
    }

    public void SetState(SessionState sessionState)
    {
        _state = sessionState;
    }

    public void SetClosed()
    {
        _selector.DetachSelectable(_clientSocket);
        if (_hostSocket != null) { 
            _selector.DetachSelectable(_hostSocket); 
        }
        _stateType = ISession.StateType.CLOSED;
        SessionClosed?.Invoke(this);
    }

    public void Dispose()
    {
        _clientSocket.Dispose();
        _hostSocket?.Dispose();
    }

    public void HandleRead(Socket readableSocket)
    {
        _state.HandleRead(readableSocket);
    }

    public void HandleWrite(Socket writableSocket)
    {
        _state.HandleWrite(writableSocket);
    }
}
