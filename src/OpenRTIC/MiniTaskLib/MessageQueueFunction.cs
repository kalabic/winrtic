using System.Threading.Channels;
using System.Threading.Tasks;

namespace OpenRTIC.MiniTaskLib;

public abstract class MessageQueueFunction<TMessage> : DisposableStream, IQueueWriter<TMessage>
{
    private object _channelLock = new object();

    private Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>();

    private bool _channelIsComplete = false;

    private CancellationToken _cancellation;

    private string _label = "";

    private ActionTask? _asyncTask = null;

    public bool IsMessageQueueComplete { get { return _channelIsComplete; } }

    public string QueueLabel { get { return _label; } }


    public MessageQueueFunction(CancellationToken cancellation)
    {
        this._cancellation = cancellation;
    }

    protected void SetLabel(string label)
    {
        this._label = label;
    }

    /// <summary>
    /// Channel lock (<see cref="_channelLock">) needs to be aquired before invoking this.
    /// </summary>
    /// <returns></returns>
    private bool TryCompleteWriter()
    {
        if (!_channelIsComplete)
        {
            _channelIsComplete = _channel.Writer.TryComplete();
        }
        return _channelIsComplete;
    }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            lock (_channelLock)
            {
                TryCompleteWriter();
            }
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    override public void Flush() => throw new NotImplementedException();

    virtual public ActionTask? GetAwaiter()
    {
        return _asyncTask;
    }

    protected void MessageQueueEntry(CancellationToken cancellation)
    {
        try
        {
            TaskFunction(cancellation);
        }
        catch (TaskCanceledException) { }
    }

    protected void MessageQueueEntryAsync(CancellationToken cancellation)
    {
        var queueTask = TaskFunctionAsync(cancellation);
        _asyncTask = new ActionTask( (actionCancellation) =>
        {
            try
            {
                queueTask.Wait(actionCancellation);
            }
            catch (TaskCanceledException) { }
        });
#if DEBUG
        _asyncTask.SetLabel("Awaiter for " + _label);
#endif
        _asyncTask.Start();
    }

    protected void TaskFunction(CancellationToken cancellation)
    {
        while (_channel.Reader.WaitToReadAsync(cancellation).AsTask().GetAwaiter().GetResult())
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
        return result;
    }

    public bool TryWriteFinalMessage(TMessage message)
    {
        bool result = false;
        lock (_channelLock)
        {
            if (!_channelIsComplete)
            {
                result = _channel.Writer.TryWrite(message);
                TryCompleteWriter();
            }
        }
        return result;
    }
}
