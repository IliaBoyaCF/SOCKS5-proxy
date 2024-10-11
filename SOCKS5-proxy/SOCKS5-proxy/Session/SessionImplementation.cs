using System.Net.Sockets;

namespace SOCKS5_proxy.Session;

public class SessionImplementation : ISession
{
    public ISession.StateType State { get { return _stateType; } }

    public event ISession.OnSessionClose? SessionClosed;

    private readonly Socket _clientSocket;
    private readonly ISubscibableSelector _selector;
    private Socket? _hostSocket;

    private SessionState _state;
    private ISession.StateType _stateType = ISession.StateType.ACTIVE;

    public SessionImplementation(Socket clientSocket, ISubscibableSelector selector)
    {
        _clientSocket = clientSocket;
        _selector = selector;
        _state = new NegotiationState(this, _clientSocket, _selector);
    }

    public void SetState(SessionState sessionState)
    {
        _state = sessionState;
    }

    public void OnHostConnection(Socket hostSocket)
    {
        _hostSocket = hostSocket;
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
}
