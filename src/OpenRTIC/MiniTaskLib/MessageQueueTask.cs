using System.Threading.Channels;
using OpenRTIC.BasicDevices;

namespace OpenRTIC.MiniTaskLib;

public abstract class MessageQueueTask<TMessage> : TaskWithEvents
{
    private object _channelLock = new object();

    private Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>();

    private bool _channelIsComplete = false;


    public bool IsMessageQueueComplete { get { return _channelIsComplete; } }


    public MessageQueueTask(CancellationToken? cancellation = null)
        : base(cancellation)
    { }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            lock(_channelLock)
            {
                if (!_channelIsComplete)
                {
                    _channelIsComplete = _channel.Writer.TryComplete();
                }
            }
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    override protected void TaskFunction(CancellationToken cancellation)
    {
        TaskFunctionAsync(cancellation).Wait();
    }

    protected async Task TaskFunctionAsync(CancellationToken cancellation)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellation))
        {
            while (_channel.Reader.TryRead(out TMessage? message))
            {
                if (message is not null)
                {
                    ProcessMessage(message);
                }
            }
        }
    }

    abstract protected void ProcessMessage(TMessage message);

    public bool TryWrite(TMessage message)
    {
        bool result = false;
        lock (_channelLock)
        {
            if (!_channelIsComplete)
            {
                result = _channel.Writer.TryWrite(message);
            }
        }
#if DEBUG
        if (!result)
        {
            DeviceNotifications.Error("[ '" + TaskLabel + "' - TryWrite Failed ]");
        }
#endif
        return result;
    }
}
