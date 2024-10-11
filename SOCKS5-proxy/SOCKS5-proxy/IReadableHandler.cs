using System.Net.Sockets;

namespace SOCKS5_proxy;

public interface IReadableHandler
{
    void HandleRead(Socket readableSocket);
}