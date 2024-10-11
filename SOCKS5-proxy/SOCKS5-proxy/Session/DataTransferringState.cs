using System.Net.Sockets;

namespace SOCKS5_proxy.Session;

internal class DataTransferringState : SessionState
{
    private readonly Socket _clientSocket;
    private readonly Socket _hostSocket;
    private readonly ISubscibableSelector _selector;
    private readonly SessionImplementation _session;

    public DataTransferringState(Socket clientSocket, Socket hostSocket, ISubscibableSelector selector, SessionImplementation session)
    {
        int tmp = clientSocket.Available;
        if (session.State == ISession.StateType.CLOSED)
        {
            throw new InvalidOperationException("Can't set transferring state when session is closed.");
        }
        Console.WriteLine("Session is set to data transferring state.");
        _clientSocket = clientSocket;
        _hostSocket = hostSocket;
        _selector = selector;
        _session = session;
        _selector.AttachSelectable(_hostSocket, (ISubscibableSelector.ReadableHandler)HandleRead);
        _selector.AttachSelectable(_clientSocket, (ISubscibableSelector.ReadableHandler)HandleRead);
    }

    public override void HandleRead(Socket readableSocket)
    {
        Socket destSocket = _hostSocket;
        if (readableSocket == _hostSocket)
        {
            destSocket = _clientSocket;
        }
        if (_selector.IsAttached(destSocket))
        {
            if (_selector.GetAttachedType(destSocket) == ISubscibableSelector.SelectableType.WRITEABLE || _selector.GetAttachedType(destSocket) == ISubscibableSelector.SelectableType.READ_WRITABLE)
            {
                return;
            }
        }
        byte[] bytes;
        try
        {
            bytes = new byte[readableSocket.Available];
            readableSocket.Receive(bytes);
        }
        catch (SocketException ex)
        {
            _session.SetClosed();
            return;
        }
        try
        {
            int tmp = destSocket.Available;
        }
        catch (ObjectDisposedException)
        {
            _session.SetClosed();
            return;
        }
        
        new NonBlockingWriter(destSocket, _selector, bytes, (e) => {
            if (e != null)
            {
                _session.SetClosed();
                return;
            }
        });
    }

    public override void HandleWrite(Socket writableSocket)
    {
        throw new NotImplementedException();
    }
}
