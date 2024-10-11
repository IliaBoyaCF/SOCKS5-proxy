using System.Net.Sockets;

namespace SOCKS5_proxy;

public interface IWritableHandler
{
    void HandleWrite(Socket writableSocket);
}