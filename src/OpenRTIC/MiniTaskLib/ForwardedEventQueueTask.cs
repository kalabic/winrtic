using System.Collections.ObjectModel;

namespace OpenRTIC.MiniTaskLib;

public class ForwardedEventQueueTask : MessageQueueTask<IInvokeForwardedEvent>
{
    private Collection<EventForwarderBase> _forwarders = new();

    public ForwardedEventQueueTask()
        : base()
    { }

    public ForwardedEventQueueTask(CancellationToken cancellation)
        : base(cancellation)
    { }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            _forwarders.Clear();
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    protected override void ProcessMessage(IInvokeForwardedEvent message)
    {
        message.InvokeForwardedEvent();
    }

    public EventForwarder<TMessage> NewQueuedEventHandler<TMessage>()
    {
        var forwarder = new EventForwarder<TMessage>(this);
        if (!IsMessageQueueComplete)
        {
            _forwarders.Add(forwarder);
        }
        return forwarder;
    }

    public EventForwarder<TMessage> NewQueuedEventForwarder<TMessage>(EventHandler<TMessage> eventHandler)
    {
        var forwarder = new EventForwarder<TMessage>(this, eventHandler);
        if (!IsMessageQueueComplete)
        {
            _forwarders.Add(forwarder);
        }
        return forwarder;
    }

    public bool EnqueueAndInvoke<TMessage>(TMessage argument, Action<TMessage> action)
    {
        if (_forwarders.Count > 0)
        {
            var message = new MessageAction<TMessage>(action, argument);
            return base.TryWrite(message);
        }

        return false;
    }
}
