using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System.Net;
using System.Net.Sockets;

namespace SOCKS5_proxy;

public class DNSResolver
{

    private static DNSResolver _instance;
    private static int s_defaultTTL;
    private EndPoint s_dnsServerEndPoint = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);

    public static DNSResolver Instance
    {
        get
        {
            _instance ??= new DNSResolver();
            return _instance;
        }
    }

    public delegate void OnResolve(IList<IResourceRecord> hostAddresses);

    private record DnsRequest(string address, OnResolve onResolve);

    private readonly Socket _socket;
    private readonly Queue<DnsRequest> _requests = [];
    private readonly Dictionary<string, DnsRequest> _waitingReply = [];
    private readonly Dictionary<string, IList<IResourceRecord>> _cache = [];

    private DNSResolver() 
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    public void AttachOnSelector(ISubscibableSelector selector)
    {
        selector.AttachSelectable(_socket, HandleReply, SendNextRequest);
    }

    private void HandleReply(Socket socket)
    {
        if (socket != _socket)
        {
            throw new InvalidOperationException("Socket on selector must me the same socket as DNSResolver has.");
        }
        byte[] bytes = new byte[_socket.Available];
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        _socket.ReceiveFrom(bytes, ref endPoint);
        IResponse response = Response.FromArray(bytes);

        string resolvedDomainName = response.AnswerRecords[0].Name.ToString();

        if (!_cache.ContainsKey(resolvedDomainName))
        {
            _cache.Add(resolvedDomainName, response.AnswerRecords);
        }

        _waitingReply.GetValueOrDefault(resolvedDomainName)?.onResolve(response.AnswerRecords);

        _waitingReply.Remove(resolvedDomainName);

    }

    private void SendNextRequest(Socket socket)
    {
        if (socket != _socket)
        {
            throw new InvalidOperationException("Socket on selector must me the same socket as DNSResolver has.");
        }
        if (_requests.Count == 0)
        {
            return;
        }
        SendRequest(_requests.Dequeue());
    }

    private void SendRequest(DnsRequest dnsRequest)
    {
        List<Question> questions = [new Question(Domain.FromString(dnsRequest.address), RecordType.A, RecordClass.IN)];
        Request request = new(new Header(), questions, [])
        {
            RecursionDesired = true,
        };

        _socket.SendTo(request.ToArray(), s_dnsServerEndPoint);

    }

    public void RequestResolve(string hostName, OnResolve onResolve)
    {
        if (_cache.ContainsKey(hostName))
        {
            onResolve(_cache.GetValueOrDefault(hostName));
            return;
        }
        _requests.Enqueue(new DnsRequest(hostName, onResolve));
        //if (_requests.Count == 1)
        //{
        //    throw new NotImplementedException();
        //}
    }



}