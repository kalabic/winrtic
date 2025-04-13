using OpenRTIC.BasicDevices;

namespace OpenRTIC.MiniTaskLib;

public class EventForwarder<TEventArgs> : IInvokeEvent<TEventArgs>
{
    private IQueueWriter<IInvokeForwardedEvent>? destinationQueue = null;

    public event EventHandler<TEventArgs>? eventProxy;

    public EventForwarder(IQueueWriter<IInvokeForwardedEvent> destinationQueue)
    {
        this.destinationQueue = destinationQueue;
    }

    public EventForwarder(IQueueWriter<IInvokeForwardedEvent> destinationQueue, EventHandler<TEventArgs> destinationHandler)
        : this(destinationQueue)
    {
        this.eventProxy += destinationHandler;
    }

    public EventForwarder(IQueueWriter<IInvokeForwardedEvent> destinationQueue, EventContainer<TEventArgs> destinationHandler)
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
        destinationQueue?.TryWrite(new Message<TEventArgs>(this, ev));
    }

    override public void Invoke(TEventArgs ev)
    {
        // TODO: Exceptions here are dangerous and sneaky.
        try
        {
            eventProxy?.Invoke(null, ev);
        }
        catch (Exception ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
        }
    }
}
