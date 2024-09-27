namespace SOCKS5_proxy.Protocol;

public class ServerNegotiationReply : Message
{
    public static readonly int s_MinSize = 2;
    public static readonly int s_MaxSize = 2;
    public static readonly int s_ConstSize = 2;

    public ClientNegotiationRequest.NegotiationMethod SelectedMethod { get; }

    public ServerNegotiationReply(ClientNegotiationRequest.NegotiationMethod selectedMethod)
    {
        SelectedMethod = selectedMethod;
        serialized = [VER_FIELD, (byte) SelectedMethod];
    }
}
