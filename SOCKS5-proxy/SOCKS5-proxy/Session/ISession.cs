namespace SOCKS5_proxy.Session;

public interface ISession : IDisposable, IWritableHandler, IReadableHandler
{
    public StateType State { get; }

    public delegate void OnSessionClose(ISession state);

    public event OnSessionClose SessionClosed;

    public enum StateType
    {
        ACTIVE,
        CLOSED,
    }
}
