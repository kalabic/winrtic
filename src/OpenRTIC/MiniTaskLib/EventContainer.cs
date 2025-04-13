using OpenRTIC.BasicDevices;

namespace OpenRTIC.MiniTaskLib;


public interface IEventContainer
{
    public IEventContainer NewCompatibleInstance();
}

public class EventContainer<TMessage> : IEventContainer
{
    /// <summary>
    /// No difference between async and normal event handlers except async event handlers will be invoked first.
    /// </summary>
    public event EventHandler<TMessage>? _asyncEvent;

    public event EventHandler<TMessage>? _event;

    public void Invoke(TMessage update, object? sender = null)
    {
        // TODO: Exceptions from inside invoked event handlers are dangerous and sneaky and break everything.
        try
        {
            _asyncEvent?.Invoke(sender, update);
            _event?.Invoke(sender, update);
        }
        catch (Exception ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
        }
    }

    /// <summary>
    /// WARNING about this method: All event handlers, async or not, using methods from this class are queued first!
    /// Think if given even handler can simply be directly attached to source instead of queued here.
    /// 
    /// <para>This exists to automatically run CPU-heavy handler in its own Task after it was read from queue.</para>
    /// 
    /// <para>No difference between async and normal event handlers except async event handlers will be invoked first.</para>
    /// </summary>
    /// <param name="eventHandler"></param>
    public void ConnectEventHandlerAsync(EventHandler<TMessage> eventHandler)
    {
        this._asyncEvent += eventHandler;
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
