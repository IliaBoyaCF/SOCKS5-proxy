namespace SOCKS5_proxy.Protocol;

public abstract class Message
{
    public const byte VER_FIELD = 0x05;
    public const byte RSV_FIELD = 0x00;

    public enum MessageType
    {
        CLIENT_NEGOTIATION_REQUEST,
        SERVER_NEGOTIATION_REPLY,
        CLIENT_REQUEST,
        SERVER_REPLY,
    }

    protected byte[] serialized = [];
    public virtual byte[] Serialize()
    {
        return serialized;
    }

}
