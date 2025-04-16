using System.Collections.ObjectModel;

namespace OpenRTIC.MiniTaskLib;

public class ForwardedEventQueue : MessageQueueFunction<IInvokeForwardedEvent>
{
    private Collection<EventForwarderBase> _forwarders = new();

    public EventCollection TaskEvents { get { return _events; } }


    private EventCollection _events = new();

    private CancellationTokenSource _cancellationSource;


    public ForwardedEventQueue()
        : this(new CancellationTokenSource()) { }

    private ForwardedEventQueue(CancellationTokenSource cancellationSource)
        : base(cancellationSource.Token)
    {
        this._cancellationSource = cancellationSource;

        // TODO: Add an API for following steps:
        _events.EnableInvokeFor<CloseMessageQueue>();
        _events.Connect(
            NewQueuedEventForwarder<CloseMessageQueue>(
                (_, update) => TryWriteFinalMessage(
                    new FinalMessage( 
                        () => { 
                            _cancellationSource.Cancel(); 
                        } ))));
    }

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
