using System.Net.Sockets;
using static SOCKS5_proxy.Selector;

namespace SOCKS5_proxy;

public interface ISubscibableSelector
{
    public delegate void ReadableHandler(Socket socket);

    public delegate void WritableHandler(Socket socket);

    public void AttachSelectable(Socket socket, ReadableHandler readableHandler);

    public void AttachSelectable(Socket socket, WritableHandler writableHandler);

    public void AttachSelectable(Socket socket, ReadableHandler readableHandler, WritableHandler writableHandler);

    public void AttachConnectable(Task connectionTask, OnConnection onConnection);

    public bool IsAttached(Socket socket);

    public SelectableType GetAttachedType(Socket socket);

    public SelectableHandlers DetachSelectable(Socket socket, SelectableType type);
    void PrintDebug();
    void DetachSelectable(Socket socket);

    public record SelectableHandlers(SelectableType Type, ReadableHandler? ReadableHandler, WritableHandler? WritableHandler)
    {
        public static SelectableHandlers NewHandler(ReadableHandler readableHandler)
        {
            return new SelectableHandlers(SelectableType.READABLE, readableHandler, null);
        }

        public static SelectableHandlers NewHandler(WritableHandler writableHandler)
        {
            return new SelectableHandlers(SelectableType.WRITEABLE, null, writableHandler);
        }

        public static SelectableHandlers NewHandler(ReadableHandler readableHandler, WritableHandler writableHandler)
        {
            return new SelectableHandlers(SelectableType.READ_WRITABLE, readableHandler, writableHandler);
        }
    }
    public enum SelectableType
    {
        READABLE,
        WRITEABLE,
        READ_WRITABLE,
    }
}