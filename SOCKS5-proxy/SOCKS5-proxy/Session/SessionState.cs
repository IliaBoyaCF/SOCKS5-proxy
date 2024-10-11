using System.Net.Sockets;

namespace SOCKS5_proxy.Session;

public abstract class SessionState
{
    protected class NonBlockingReceiver
    {
        public delegate void OnComplete(byte[]? bytes, Exception? exception);

        private readonly ISubscibableSelector _selector;
        private readonly int _requiredBytesToRead;
        private readonly OnComplete _onComplete;
        private readonly byte[] _buffer;

        private int _receivedBytes = 0;

        public NonBlockingReceiver(Socket socket, ISubscibableSelector selector, int N, OnComplete onComplete)
        {
            _selector = selector;
            if (_selector.IsAttached(socket))
            {
                ISubscibableSelector.SelectableType selectableType = _selector.GetAttachedType(socket);
                if (selectableType == ISubscibableSelector.SelectableType.READABLE || 
                    selectableType == ISubscibableSelector.SelectableType.READ_WRITABLE)
                { 
                    throw new ArgumentException("Socket is already attached on read by someone."); 
                }
            }
            _selector.AttachSelectable(socket, (ISubscibableSelector.ReadableHandler)HandleRead);
            _requiredBytesToRead = N;
            _buffer = new byte[N];
            _onComplete = onComplete;
        }

        private void HandleRead(Socket socket)
        {
            int available = socket.Available;
            int toRead = available;
            if (available > _requiredBytesToRead)
            {
                toRead = _requiredBytesToRead;
            }
            int readBytes;
            try
            {
                readBytes = socket.Receive(_buffer, _receivedBytes, toRead, SocketFlags.None);
            }
            catch (SocketException e)
            {
                _selector.DetachSelectable(socket, ISubscibableSelector.SelectableType.READABLE);
                _onComplete(null, e);
                return;
            }
            _receivedBytes += readBytes;
            if (_receivedBytes == _requiredBytesToRead)
            {
                _selector.DetachSelectable(socket, ISubscibableSelector.SelectableType.READABLE);
                _onComplete(_buffer, null);
            }
        }
    }

    protected class NonBlockingWriter
    {
        public delegate void OnComplete(Exception? exception);

        private readonly ISubscibableSelector _selector;
        private readonly OnComplete _onComplete;
        private readonly byte[] _buffer;

        private int _sentBytes = 0;

        public NonBlockingWriter(Socket socket, ISubscibableSelector selector, byte[] bytes, OnComplete onComplete)
        {
            _selector = selector;
            if (_selector.IsAttached(socket))
            {
                ISubscibableSelector.SelectableType selectableType = _selector.GetAttachedType(socket);
                if (selectableType == ISubscibableSelector.SelectableType.WRITEABLE ||
                    selectableType == ISubscibableSelector.SelectableType.READ_WRITABLE)
                {
                    throw new ArgumentException("Socket is already attached on write by someone.");
                }
            }
            _selector.AttachSelectable(socket, (ISubscibableSelector.WritableHandler)HandleWrite);
            _buffer = bytes;
            _onComplete = onComplete;
        }

        private void HandleWrite(Socket socket)
        {
            int sentBytes;
            try
            {
                sentBytes = socket.Send(_buffer, _sentBytes, _buffer.Length - _sentBytes, SocketFlags.None);
            }
            catch (SocketException e)
            {
                _selector.DetachSelectable(socket, ISubscibableSelector.SelectableType.WRITEABLE);
                _onComplete(e);
                return;
            }
            _sentBytes += sentBytes;
            if (_sentBytes == _buffer.Length)
            {
                _selector.DetachSelectable(socket, ISubscibableSelector.SelectableType.WRITEABLE);
                _onComplete(null);
            }
        }
    }

    public abstract void HandleRead(Socket readableSocket);

    public abstract void HandleWrite(Socket writableSocket);
}