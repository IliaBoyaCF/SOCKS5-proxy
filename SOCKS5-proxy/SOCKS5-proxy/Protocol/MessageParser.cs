using System.Net;

namespace SOCKS5_proxy.Protocol;

public class MessageParser
{
    public static ClientNegotiationRequest ParseClientNegotiationRequest(byte[] bytes)
    {
        Validate(bytes, ClientNegotiationRequest.s_MinSize, ClientNegotiationRequest.s_MaxSize);
        List<ClientNegotiationRequest.NegotiationMethod> negotiationMethods = [];
        for (int i = 0; i < bytes[1]; ++i)
        {
            negotiationMethods.Add((ClientNegotiationRequest.NegotiationMethod)bytes[i + 2]);
        }
        return new ClientNegotiationRequest(negotiationMethods);
    }

    public static ServerNegotiationReply ParseServerNegotiationReply(byte[] bytes)
    {
        Validate(bytes, ServerNegotiationReply.s_MinSize, ServerNegotiationReply.s_MaxSize);
        return new ServerNegotiationReply((ClientNegotiationRequest.NegotiationMethod)bytes[1]);
    }

    public static ClientRequest ParseClientRequest(byte[] bytes)
    {
        Validate(bytes, ClientRequest.s_MinSize, ClientRequest.s_MaxSize);

        ClientRequest.AddressType addressType = (ClientRequest.AddressType)bytes[3];
        IPAddress address;
        int port = 0;
        switch (addressType)
        {
            case ClientRequest.AddressType.IPv4:
                address = new(new ArraySegment<byte>(bytes, 4, 4).ToArray());
                port = ParsePort(bytes, 8);
                return new ClientRequest((ClientRequest.Command)bytes[1], address, port);

            case ClientRequest.AddressType.IPv6:
                address = new(new ArraySegment<byte>(bytes, 4, 16).AsSpan().ToArray());
                port = ParsePort(bytes, 12);
                return new ClientRequest((ClientRequest.Command)bytes[1], address, port);
            case ClientRequest.AddressType.DOMAIN_NAME:
                string domainName = System.Text.Encoding.UTF8.GetString(new ArraySegment<byte>(bytes, 5, bytes[4]).ToArray());
                port = ParsePort(bytes, 5 + bytes[4]);
                return new ClientRequest((ClientRequest.Command)bytes[1], domainName, port);
            default:
                throw new ArgumentException("Invalid address type.");
        }
    }

    private static int ParsePort(byte[] bytes, int startingIndex)
    {
        int port;
        byte[] portBytes = new ArraySegment<byte>(bytes, startingIndex, 2).ToArray();

        if (!BitConverter.IsLittleEndian)
        {
            return BitConverter.ToUInt16(portBytes, 0);
        }

        (portBytes[1], portBytes[0]) = (portBytes[0], portBytes[1]);
        port = BitConverter.ToUInt16(portBytes, 0);
        return port;
    }

    public static ServerReply ParseServerReply(byte[] bytes)
    {
        Validate(bytes, ClientRequest.s_MinSize, ClientRequest.s_MaxSize);

        ClientRequest.AddressType addressType = (ClientRequest.AddressType)bytes[3];
        IPAddress address;
        int port;
        switch (addressType)
        {
            case ClientRequest.AddressType.IPv4:
                address = new(new ArraySegment<byte>(bytes, 4, 4).AsSpan().ToArray());
                port = BitConverter.ToInt16(bytes, 8);
                return new ServerReply((ServerReply.Reply)bytes[1], address, port);

            case ClientRequest.AddressType.IPv6:
                address = new(new ArraySegment<byte>(bytes, 4, 8).AsSpan().ToArray());
                port = BitConverter.ToInt16(bytes, 12);
                return new ServerReply((ServerReply.Reply)bytes[1], address, port);
            case ClientRequest.AddressType.DOMAIN_NAME:
                string domainName = new(new ArraySegment<byte>(bytes, 5, bytes[4]).ToString());
                port = BitConverter.ToInt16(bytes, 5 + bytes[4]);
                return new ServerReply((ServerReply.Reply)bytes[1], domainName, port);
            default:
                throw new ArgumentException("Invalid address type.");
        }
    }

    private static void Validate(byte[] bytes, int minSize, int maxSize)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }
        if (!(bytes.Length >= minSize && bytes.Length <= maxSize))
        {
            throw new ArgumentException("Provided sequence is not a message.");
        }
    }
}
