namespace OpenRTIC.MiniTaskLib;

public interface IEventContainer
{
    public IEventContainer NewCompatibleInstance();
}

public class EventContainer<TMessage> : IEventContainer
{
    public event EventHandler<TMessage>? _event;

    public void Invoke(TMessage update, object? sender = null)
    {
        _event?.Invoke(sender, update);
    }

    public void ConnectEventHandler(EventHandler<TMessage> eventHandler)
    {
        this._event += eventHandler;
    }

    public void ConnectForwarder(EventForwarder<TMessage> forwarder)
    {
        this._event += forwarder.GetNewEventQueueWriter();
    }

    public IEventContainer NewCompatibleInstance()
    {
        return new EventContainer<TMessage>();
    }
}
