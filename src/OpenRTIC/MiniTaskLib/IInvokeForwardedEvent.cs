namespace OpenRTIC.MiniTaskLib;


// Needed for Collection<EventForwarderBase>, it accepts only non-generic interfaces and classes.
public class EventForwarderBase { }


public abstract class IInvokeEvent<TEventArgs> : EventForwarderBase
{
    abstract public void Invoke(TEventArgs ev);
}


public abstract class IInvokeForwardedEvent
{
    abstract public void InvokeForwardedEvent();
}


public class Message<TEventArgs> : IInvokeForwardedEvent
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

public class FinalMessage : IInvokeForwardedEvent
{
    private Action _finalAction;

    public FinalMessage(Action finalAction) 
    {
        this._finalAction = finalAction;
    }

    override public void InvokeForwardedEvent() => _finalAction();
}


public interface IQueueWriter<TMessage>
{
    public bool TryWrite(TMessage message);

    public bool TryWriteFinalMessage(TMessage message);
}

public class MessageAction<TMessage> : IInvokeForwardedEvent
{
    private Action<TMessage> action;

    private TMessage argument;

    public MessageAction(Action<TMessage> action, TMessage argument)
    {
        this.action = action;
        this.argument = argument;
    }

    override public void InvokeForwardedEvent()
    {
        action(argument);
    }
}
