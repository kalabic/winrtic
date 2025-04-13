namespace OpenRTIC.MiniTaskLib;

// Needed for Collection<EventForwarderBase>, it accepts only non-generic interfaces and classes.
public class EventForwarderBase { }

public abstract class IInvokeEvent<TEventArgs> : EventForwarderBase
{
    abstract public void Invoke(TEventArgs ev);
}

public class EventForwarder<TEventArgs> : IInvokeEvent<TEventArgs>
{
    private class Message : ForwardedEventQueue.IInvokeForwardedEvent
    {
        IInvokeEvent<TEventArgs> eventProxy;

        TEventArgs args;

        public Message(EventForwarder<TEventArgs> eventProxy, TEventArgs args)
        {
            this.eventProxy = eventProxy;
            this.args = args;
        }

        override public void InvokeForwardedEvent()
        {
            eventProxy.Invoke(args);
        }
    }

    private ForwardedEventQueue? destinationQueue = null;

    public event EventHandler<TEventArgs>? eventProxy;

    public EventForwarder(ForwardedEventQueue destinationQueue)
    {
        this.destinationQueue = destinationQueue;
    }

    public EventForwarder(ForwardedEventQueue destinationQueue, EventHandler<TEventArgs> destinationHandler)
        : this(destinationQueue)
    {
        this.eventProxy += destinationHandler;
    }

    public EventForwarder(ForwardedEventQueue destinationQueue, EventContainer<TEventArgs> destinationHandler)
        : this(destinationQueue)
    {
        this.eventProxy += (new EventHandler<TEventArgs>( (_, ev) => { destinationHandler.Invoke(ev); }));
    }

    public EventHandler<TEventArgs> GetNewEventQueueWriter()
    {
        return new EventHandler<TEventArgs>((sender, ev) => WriteToEventQueue(sender, ev));
    }

    private void WriteToEventQueue(object? sender, TEventArgs ev)
    {
        destinationQueue?.TryWrite(new Message(this, ev));
    }

    override public void Invoke(TEventArgs ev)
    {
        eventProxy?.Invoke(null, ev);
    }
}
