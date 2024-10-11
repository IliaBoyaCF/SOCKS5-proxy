using SOCKS5_proxy.Protocol;
using System.Net.Sockets;

namespace SOCKS5_proxy.Session;

internal class NegotiationState : SessionState
{
    private readonly SessionImplementation _session;

    private Socket _clientSocket;
    private ISubscibableSelector _selector;
    private byte[] _buffer = new byte[2];

    public NegotiationState(SessionImplementation session, Socket clientSocket, ISubscibableSelector selector)
    {
        _session = session;
        _clientSocket = clientSocket;
        _selector = selector;
        new NonBlockingReceiver(_clientSocket, _selector, 2, (bytes, exception) =>
        {
            if (exception != null)
            {
                _session.SetClosed();
                return;
            }
            for (int i = 0; i < bytes.Length; i++)
            {
                _buffer[i] = bytes[i];
            }
            new NonBlockingReceiver(clientSocket, _selector, _buffer[1], (arrMethods, e) =>
            {
                if (e != null)
                {
                    _session.SetClosed();
                    return;
                }
                _buffer = _buffer.Concat(arrMethods).ToArray();
                OnRequestGet(MessageParser.ParseClientNegotiationRequest(_buffer));
            });
        });
    }

    private void OnRequestGet(ClientNegotiationRequest clientNegotiationRequest)
    {
        if (!clientNegotiationRequest.NegotiationMethods.Contains(ClientNegotiationRequest.NegotiationMethod.NO_AUTHENTICATION_REQUIRED))
        {
            new NonBlockingWriter(_clientSocket, _selector,
                new ServerNegotiationReply(ClientNegotiationRequest.NegotiationMethod.NO_ACCEPTABLE_METHODS).Serialize(), (e) => _session.SetClosed());
        }
        new NonBlockingWriter(_clientSocket, _selector,
                new ServerNegotiationReply(ClientNegotiationRequest.NegotiationMethod.NO_AUTHENTICATION_REQUIRED).Serialize(), (e) =>
                {
                    if (e != null)
                    {
                        _session.SetClosed();
                        return;
                    }
                    _session.SetState(new RequestAcceptState(_session, _clientSocket, _selector));
                });
    }
}