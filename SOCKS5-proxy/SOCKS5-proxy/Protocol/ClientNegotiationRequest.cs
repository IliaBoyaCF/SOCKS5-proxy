using System.Collections.ObjectModel;

namespace SOCKS5_proxy.Protocol;

public class ClientNegotiationRequest : Message
{
    public enum NegotiationMethod : byte
    {
        NO_AUTHENTICATION_REQUIRED,
        GSSAPI,
        USERNAME_PASSWORD,
        NO_ACCEPTABLE_METHODS = 0xff,
    }

    public static readonly int s_MinSize = 3;
    public static readonly int s_MaxSize = 257;

    private static readonly int _headerFieldsCount = 2;

    public ReadOnlyCollection<NegotiationMethod> NegotiationMethods { get => _negotiationMethods.AsReadOnly(); };

    private List<NegotiationMethod> _negotiationMethods = [];

    public ClientNegotiationRequest(IList<NegotiationMethod> methods)
    {
        _negotiationMethods = new List<NegotiationMethod>(methods);
        serialized = new byte[_headerFieldsCount + _negotiationMethods.Count];
        serialized[0] = VER_FIELD;
        serialized[1] = (byte)_negotiationMethods.Count;
        for (int i = 0; i < _negotiationMethods.Count; i++)
        {
            serialized[i + _headerFieldsCount] = (byte) _negotiationMethods[i];
        }
    }
}
