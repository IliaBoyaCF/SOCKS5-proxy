using System.Buffers;
using System.Net;
using System.Text;

namespace SOCKS5_proxy.Protocol;

public class ClientRequest : Message
{
    public static readonly int s_MinSize = 6 + 2;
    public static readonly int s_MaxSize = 6 + 256;

    public enum AddressType : byte
    {
        IPv4 = 0x01,
        IPv6 = 0x04,
        DOMAIN_NAME = 0x03,
    }

    public enum Command : byte
    {
        CONNECT = 0x01,
        BIND = 0x02,
        UPD_ASSOSIATE = 0x03,
    }

    public AddressType SelectedAddressType { get; }

    public string Address { init; get; }
    public int Port { init; get; }

    public ClientRequest(Command command, string hostName, int hostPort) 
    {
        
        if (hostName.Length > byte.MaxValue)
        {
            throw new ArgumentException("Host name is too long.");
        }

        SelectedAddressType = AddressType.DOMAIN_NAME;

        Address = hostName;
        Port = hostPort;

        ArrayBufferWriter<byte> arrayBufferWriter = new();

        arrayBufferWriter.Write([VER_FIELD, (byte)command, RSV_FIELD, (byte)AddressType.DOMAIN_NAME]);
        arrayBufferWriter.Write([(byte)hostName.Length]);
        arrayBufferWriter.Write(Encoding.UTF8.GetBytes(hostName).AsSpan());
        arrayBufferWriter.Write(BitConverter.GetBytes((short)hostPort).AsSpan());

        serialized = arrayBufferWriter.WrittenSpan.ToArray();
    }

    public ClientRequest(Command command, IPAddress hostAddress, int hostPort)
    {
        Address = hostAddress.ToString();
        Port = hostPort;

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

        SelectedAddressType = addressType;

        arrayBufferWriter.Write([VER_FIELD, (byte)command, RSV_FIELD, (byte)addressType]);
        arrayBufferWriter.Write(hostAddress.GetAddressBytes());
        arrayBufferWriter.Write(BitConverter.GetBytes((short)hostPort).AsSpan());

        serialized = arrayBufferWriter.WrittenSpan.ToArray();
    }
}
