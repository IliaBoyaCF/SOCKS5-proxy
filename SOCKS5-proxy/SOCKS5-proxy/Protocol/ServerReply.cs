using System.Buffers;
using System.Net;
using System.Text;

namespace SOCKS5_proxy.Protocol;

public class ServerReply : Message
{
    public static readonly int s_MinSize = 6 + 2;
    public static readonly int s_MaxSize = 6 + 256;

    public enum AddressType : byte
    {
        IPv4 = 0x01,
        IPv6 = 0x04,
        DOMAIN_NAME = 0x03,
    }

    public enum Reply : byte
    {
        SUCCEEDED,
        GENERAL_SOCKS_SERVER_FAILURE,
        CONNECTION_NOT_ALLOWED_BY_RULESET,
        NETWORK_UNREACHABLE,
        HOST_UNREACHABLE,
        CONNECTION_REFUSED,
        TTL_EXPIRED,
        COMMAND_NOT_SUPPORTED,
        ADDRESS_TYPE_NOT_SUPPORTED,
    }

    public AddressType SelectedAddressType { get; }

    public string BindAddress { init; get; }
    public int BindPort { init; get; }

    public ServerReply(Reply reply, string hostName, int hostPort)
    {

        if (hostName.Length > byte.MaxValue)
        {
            throw new ArgumentException("Host name is too long.");
        }

        BindAddress = hostName;
        BindPort = hostPort;

        ArrayBufferWriter<byte> arrayBufferWriter = new();

        arrayBufferWriter.Write([VER_FIELD, (byte)reply, RSV_FIELD, (byte)AddressType.DOMAIN_NAME]);
        arrayBufferWriter.Write([(byte)hostName.Length]);
        arrayBufferWriter.Write(Encoding.UTF8.GetBytes(hostName).AsSpan());
        arrayBufferWriter.Write(BitConverter.GetBytes((short)hostPort).AsSpan());

        serialized = arrayBufferWriter.WrittenSpan.ToArray();
    }

    public ServerReply(Reply reply, IPAddress hostAddress, int hostPort)
    {
        BindAddress = hostAddress.ToString();
        BindPort = hostPort;

        ArrayBufferWriter<byte> arrayBufferWriter = new();

        AddressType addressType;

        switch (hostAddress.AddressFamily)
        {
            case System.Net.Sockets.AddressFamily.InterNetwork:
                addressType = AddressType.IPv4;
                break;
            case System.Net.Sockets.AddressFamily.InterNetworkV6:
                addressType = AddressType.IPv6;
                break;
            default:
                throw new ArgumentException("Unsupported address family");
        }

        arrayBufferWriter.Write([VER_FIELD, (byte)reply, RSV_FIELD, (byte)addressType]);
        arrayBufferWriter.Write(hostAddress.GetAddressBytes());
        arrayBufferWriter.Write(BitConverter.GetBytes((short)hostPort).AsSpan());

        serialized = arrayBufferWriter.WrittenSpan.ToArray();
    }
}
