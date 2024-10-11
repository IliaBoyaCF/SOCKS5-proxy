using DNS.Protocol.ResourceRecords;
using SOCKS5_proxy.Protocol;
using System.Net;
using System.Net.Sockets;

namespace SOCKS5_proxy.Session;

internal class RequestAcceptState : SessionState
{

    private readonly SessionImplementation _session;
    private readonly Socket _clientSocket;
    private readonly ISubscibableSelector _selector;

    private Socket? _hostSocket;

    public RequestAcceptState(SessionImplementation session, Socket clientSocket, ISubscibableSelector selector)
    {
        _session = session;
        _clientSocket = clientSocket;
        _selector = selector;
        new NonBlockingReceiver(_clientSocket, _selector, 4, (bytes, exception) =>
        {
            if (exception != null)
            {
                _session.SetClosed();
                return;
            }
            switch ((ClientRequest.AddressType)bytes[3])
            {
                case ClientRequest.AddressType.IPv6:
                    new NonBlockingReceiver(_clientSocket, _selector, 16 + 2, (addrBytes, e) =>
                    {
                        if (e != null)
                        {
                            _session.SetClosed();
                            return;
                        }
                        byte[] requestBytes = bytes.Concat(addrBytes).ToArray();
                        ProcessRequest(MessageParser.ParseClientRequest(requestBytes));
                    });
                    return;
                case ClientRequest.AddressType.IPv4:
                    new NonBlockingReceiver(_clientSocket, _selector, 4 + 2, (addrBytes, e) =>
                    {
                        if (e != null)
                        {
                            _session.SetClosed();
                            return;
                        }
                        byte[] requestBytes = bytes.Concat(addrBytes).ToArray();
                        ProcessRequest(MessageParser.ParseClientRequest(requestBytes));
                    });
                    return;
                case ClientRequest.AddressType.DOMAIN_NAME:
                    new NonBlockingReceiver(_clientSocket, _selector, 1, (addrBytes, e) =>
                    {
                        if (e != null)
                        {
                            _session.SetClosed();
                            return;
                        }
                        byte[] requestBytes = bytes.Concat(addrBytes).ToArray();
                        new NonBlockingReceiver(_clientSocket, _selector, requestBytes[4] + 2, (rest, exc) =>
                        {
                            if (exc != null)
                            {
                                _session.SetClosed();
                                return;
                            }
                            requestBytes = requestBytes.Concat(rest).ToArray();
                            ProcessRequest(MessageParser.ParseClientRequest(requestBytes));
                        });
                    });
                    return;
            default:
                throw new NotImplementedException();
            }
        });
    }

    private void ProcessRequest(ClientRequest request)
    {
        switch (request.SelectedAddressType)
        {
            case ClientRequest.AddressType.IPv6:
                new NonBlockingWriter(_clientSocket, _selector,
                    new ServerReply(ServerReply.Reply.ADDRESS_TYPE_NOT_SUPPORTED, request.Address, request.Port).Serialize(), (e) => {
                        _session.SetClosed();
                    });
                return;
            case ClientRequest.AddressType.IPv4:
                ConnectToHost(IPAddress.Parse(request.Address), request.Port);
                return;
            case ClientRequest.AddressType.DOMAIN_NAME:
                DNSResolver.Instance.RequestResolve(request.Address, (addresses) =>
                {
                    IPAddress selectedIP = SelectHostIp(addresses);
                    ConnectToHost(selectedIP, request.Port);
                });
                return;
            default:
                new NonBlockingWriter(_clientSocket, _selector,
                    new ServerReply(ServerReply.Reply.ADDRESS_TYPE_NOT_SUPPORTED, request.Address, request.Port).Serialize(), (e) =>
                    {
                        _session.SetClosed();
                    });
                return;
        }
    }

    private static IPAddress SelectHostIp(IList<IResourceRecord> addresses)
    {
        foreach (var address in addresses)
        {
            if (address.DataLength == 4)
            {
                return new IPAddress(address.Data);
            }
        }
        throw new ArgumentException("No addresses provided.");
    }

    private void ConnectToHost(IPAddress address, int port)
    {
        _hostSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            Blocking = false
        };
        Task task = _hostSocket.ConnectAsync(address, port);
        _selector.AttachConnectable(task, () => { SendReply(address, port); });
    }

    private void SendReply(IPAddress address, int port)
    {
        if (!_hostSocket.Connected)
        {
            new NonBlockingWriter(_clientSocket, _selector, new ServerReply(ServerReply.Reply.CONNECTION_REFUSED, address, port).Serialize(),
                (e) => {
                    _session.SetClosed();
                    });
            return;
        }
        new NonBlockingWriter(_clientSocket, _selector, new ServerReply(ServerReply.Reply.SUCCEEDED, address, port).Serialize(),
                (e) =>
                {
                    if (e != null)
                    {
                        _session.SetClosed();
                        return;
                    }
                    _session.OnHostConnection(_hostSocket);
                    _session.SetState(new DataTransferringState(_clientSocket, _hostSocket, _selector, _session));
                });
    }
}
