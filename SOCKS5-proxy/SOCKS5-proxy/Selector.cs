using System.Net.Sockets;

namespace SOCKS5_proxy;

public class Selector : ISubscibableSelector
{

    public delegate void ReadableHandler(Socket socket);

    public delegate void WritableHandler(Socket socket);

    public delegate void OnConnection();

    private readonly Dictionary<Socket, ISubscibableSelector.SelectableHandlers> _handlersBySocket = [];
    private readonly Dictionary<Task, OnConnection> _connections = [];
    private readonly List<Socket> _writeSelectable = [];
    private readonly List<Socket> _readSelectable = [];

    public void Select(int microsecTimeout)
    {
        List<Task> completedTasks = [];
        foreach (KeyValuePair<Task, OnConnection> pair in _connections)
        {
            if (pair.Key.IsCompleted)
            {
                completedTasks.Add(pair.Key);
                pair.Value();
            }
        }

        completedTasks.ForEach(task => {_connections.Remove(task); });

        List<Socket> writableSockets = new(_writeSelectable);
        List<Socket> readableSockets = new(_readSelectable);
        Socket.Select(readableSockets, writableSockets, null, microsecTimeout);
        foreach (Socket socket in writableSockets)
        {
            if (!_handlersBySocket.ContainsKey(socket))
            {
                continue;
            }
            _handlersBySocket[socket].WritableHandler(socket);
        }
        foreach (Socket socket in readableSockets)
        {
            if (!_handlersBySocket.ContainsKey(socket))
            {
                continue;
            }
            _handlersBySocket[socket].ReadableHandler(socket);
        }
    }

    public void AttachSelectable(Socket socket, ISubscibableSelector.ReadableHandler readableHandler)
    {
        if (!IsAttached(socket))
        {
            _handlersBySocket.Add(socket, ISubscibableSelector.SelectableHandlers.NewHandler(readableHandler));
            _readSelectable.Add(socket);
            return;
        }
        ISubscibableSelector.SelectableType type = GetAttachedType(socket);
        if (type == ISubscibableSelector.SelectableType.READABLE || type == ISubscibableSelector.SelectableType.READ_WRITABLE)
        {
            throw new InvalidOperationException("Other read handler is already attached.");
        }
        ISubscibableSelector.SelectableHandlers old = _handlersBySocket[socket];
        ISubscibableSelector.SelectableHandlers newHandlers = ISubscibableSelector.SelectableHandlers.NewHandler(readableHandler, old.WritableHandler);
        _handlersBySocket.Remove(socket);
        _handlersBySocket.Add(socket, newHandlers);
        _readSelectable.Add(socket);
    }

    public void AttachSelectable(Socket socket, ISubscibableSelector.WritableHandler writableHandler)
    {
        if (!IsAttached(socket))
        {
            _handlersBySocket.Add(socket, ISubscibableSelector.SelectableHandlers.NewHandler(writableHandler));
            _writeSelectable.Add(socket);
            return;
        }

        ISubscibableSelector.SelectableType type = GetAttachedType(socket);
        if (type == ISubscibableSelector.SelectableType.WRITEABLE || type == ISubscibableSelector.SelectableType.READ_WRITABLE)
        {
            throw new InvalidOperationException("Other write handler is already attached.");
        }
        ISubscibableSelector.SelectableHandlers old = _handlersBySocket[socket];
        ISubscibableSelector.SelectableHandlers newHandlers = ISubscibableSelector.SelectableHandlers.NewHandler(old.ReadableHandler, writableHandler);
        _handlersBySocket.Remove(socket);
        _handlersBySocket.Add(socket, newHandlers);
        _writeSelectable.Add(socket);
    }

    public void AttachSelectable(Socket socket, ISubscibableSelector.ReadableHandler readableHandler, 
                                                ISubscibableSelector.WritableHandler writableHandler)
    {
        if (IsAttached(socket))
        {
            throw new InvalidOperationException("Other read/write handlers are already attached.");
        }
        _handlersBySocket.Add(socket, ISubscibableSelector.SelectableHandlers.NewHandler(readableHandler, writableHandler));
        _readSelectable.Add(socket);
        _writeSelectable.Add(socket);
    }

    public void AttachConnectable(Task connectionTask, OnConnection onConnection)
    {
        _connections.Add(connectionTask, onConnection);
    }

    public bool IsAttached(Socket socket)
    {
        return _handlersBySocket.ContainsKey(socket);
    }

    public ISubscibableSelector.SelectableType GetAttachedType(Socket socket)
    {
        if (!IsAttached(socket))
        {
            throw new InvalidOperationException("Socket must be attached before calling this operation");
        }
        return _handlersBySocket[socket].Type;
    }

    public ISubscibableSelector.SelectableHandlers DetachSelectable(Socket socket, ISubscibableSelector.SelectableType type)
    {
        if (!IsAttached(socket))
        {
            throw new InvalidOperationException("Socket must be attached before calling this operation");
        }

        ISubscibableSelector.SelectableHandlers handlers = _handlersBySocket[socket];

        _handlersBySocket.Remove(socket);

        switch (type)
        {
            case ISubscibableSelector.SelectableType.READ_WRITABLE:
                _readSelectable.Remove(socket);
                _writeSelectable.Remove(socket);
                break;
            case ISubscibableSelector.SelectableType.WRITEABLE:
                _writeSelectable.Remove(socket);
                if (handlers.ReadableHandler != null)
                {
                    _handlersBySocket.Add(socket, ISubscibableSelector.SelectableHandlers.NewHandler(handlers.ReadableHandler));
                }
                break;
            case ISubscibableSelector.SelectableType.READABLE:
                _readSelectable.Remove(socket);
                if (handlers.WritableHandler != null)
                {
                    _handlersBySocket.Add(socket, ISubscibableSelector.SelectableHandlers.NewHandler(handlers.WritableHandler));
                }
                break;
            default: throw new Exception("Unknown type.");
        }

        return handlers;
    }

    public void PrintDebug()
    {
        foreach (KeyValuePair<Socket, ISubscibableSelector.SelectableHandlers> keyValuePair in _handlersBySocket)
        {
            Console.WriteLine("Remote addr: {0} read {1} write {2}", keyValuePair.Key.RemoteEndPoint, 
                keyValuePair.Value.ReadableHandler, keyValuePair.Value.WritableHandler);
        }
    }

    public void DetachSelectable(Socket socket)
    {
        if (!IsAttached(socket))
        {
            return;
        }
        ISubscibableSelector.SelectableType type = GetAttachedType(socket);
        _handlersBySocket.Remove(socket);
        switch (type)
        {
            case ISubscibableSelector.SelectableType.READABLE:
                _readSelectable.Remove(socket);
                break;
            case ISubscibableSelector.SelectableType.WRITEABLE:
                _writeSelectable.Remove(socket);
                break;
            case ISubscibableSelector.SelectableType.READ_WRITABLE:
                _readSelectable.Remove(socket);
                _writeSelectable.Remove(socket);
                break;
            default:
                throw new Exception("Unknown selectable type.");
        }
    }
}