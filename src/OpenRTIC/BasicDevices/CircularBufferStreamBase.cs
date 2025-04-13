using NAudio.Utils;
using System.Text;

namespace OpenRTIC.BasicDevices;

public class CircularBufferStreamBase : Stream, IDisposable
{
    private readonly object lockObject;

    private ManualResetEventSlim streamEvent = new ManualResetEventSlim();

    private CircularBuffer? streamBuffer = null;

    protected long _totalBytesWritten = 0;

    protected CancellationToken streamCancellation;

    public CircularBufferStreamBase(int bufferSize, CancellationToken cancellation)
    {
        lockObject = new object();
        streamBuffer = new CircularBuffer(bufferSize);
        streamCancellation = cancellation;
    }

    public CircularBufferStreamBase(int bufferSize)
        : this(bufferSize, CancellationToken.None)
    { }

    protected override void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            lock (lockObject)
            {
                if (streamBuffer is not null)
                {
                    streamBuffer.Reset();
                    streamBuffer = null;
                }
            }
#if DEBUG_VERBOSE_DISPOSE
            DeviceNotifications.ObjectDisposed(this);
#endif
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    public bool IsCancellationRequested()
    {
        return streamCancellation.IsCancellationRequested;
    }

    public void Write(byte[] buffer)
    {
        Write(buffer, 0, buffer.Length);
    }

    public void Write(string text)
    {
        if (!String.IsNullOrEmpty(text))
        {
            Write(Encoding.UTF8.GetBytes(text));
        }
    }

    public void ClearBuffer()
    {
        lock (lockObject)
        {
            streamBuffer?.Reset();
            _totalBytesWritten = 0;
        }
    }

    public int GetBufferedBytes()
    {
        return (streamBuffer is not null) ? streamBuffer.Count : 0;
    }

    public bool WaitDataAvailable(int minAvailable, int timeoutMs = 0)
    {
        int available = GetBytesAvailable(minAvailable);
        if (available >= minAvailable)
        {
            return true;
        }
        else if (available < 0 || streamCancellation.IsCancellationRequested)
        {
            // streamBuffer is null
            return false;
        }

        try
        {
            WaitHandle[] waitHandles =
            {
                streamEvent.WaitHandle,
                streamCancellation.WaitHandle
            };

            while (!streamCancellation.IsCancellationRequested)
            {
                int index = 0;
                if (timeoutMs > 0)
                {
                    index = WaitHandle.WaitAny(waitHandles, timeoutMs);
                }
                else
                {
                    index = WaitHandle.WaitAny(waitHandles);
                }

                if (index == 0)
                {
                    available = GetBytesAvailable(minAvailable);
                    if (available >= minAvailable)
                    {
                        return true;
                    }
                    else if (available < 0)
                    {
                        // streamBuffer is null
                        return false;
                    }
                }
                else if (index == 1)
                {
                    // streamCancellation.WaitHandle
                    return false;
                }
                else if (index == WaitHandle.WaitTimeout)
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
        }

        return false;
    }

    virtual protected int GetBytesAvailable(int minAvailable)
    {
        lock (lockObject)
        {
            int available = (streamBuffer is not null) ? streamBuffer.Count : -1;
            if (available < minAvailable)
            {
                streamEvent.Reset();
            }
            return available;
        }
    }

    public override bool CanRead => (streamBuffer is not null);

    public override bool CanSeek => throw new NotImplementedException();

    public override bool CanWrite => (streamBuffer is not null);

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        lock (lockObject)
        {
            if (streamBuffer is not null)
            {
                int bytesRead = streamBuffer.Read(buffer, offset, count);
                if (streamBuffer.Count == 0)
                {
                    streamEvent.Reset();
                }
                return bytesRead;
            }
        }
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        lock (lockObject)
        {
            if (streamBuffer is not null)
            {
                _totalBytesWritten += streamBuffer.Write(buffer, offset, count);
                streamEvent.Set();
            }
        }
    }
}
