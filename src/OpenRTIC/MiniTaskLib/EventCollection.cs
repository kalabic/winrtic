using System.Collections.ObjectModel;

namespace OpenRTIC.MiniTaskLib;

/// <summary>
/// All events in collection are defined by C# generics. They need to be enabled first using
/// <see cref="EnableInvokeFor"/> before <see cref="Invoke"/>
/// of an event handler will work.
/// </summary>
public class EventCollection
{
    private Collection<IEventContainer> collection = new();

    public EventCollection() { }

    public void Clear()
    {
        collection.Clear();
    }

    /// <summary>
    /// For 'Invoke' and 'ConnectEventHandler' to work for specific type of object (TMessage), it is first necessary to enable them.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <exception cref="ArgumentException"></exception>
    public void EnableInvokeFor<TMessage>()
    {
        var items = collection.OfType<EventContainer<TMessage>>();
        if (items.Count() == 0)
        {
            var item = new EventContainer<TMessage>();
            collection.Add(item);
        }
        else
        {
            throw new ArgumentException("Attempted to add pre-existing event type into EventSourceCollection.");
        }
    }

    private Collection<IEventContainer> NewCompatibleInstance()
    {
        var newInstance = new Collection<IEventContainer>();
        foreach (var item in collection)
        {
            var newItem = item.NewCompatibleInstance();
            newInstance.Add(newItem);
        }
        return newInstance;
    }

    public void MakeCompatible(EventCollection other)
    {
        collection = other.NewCompatibleInstance();
    }

    public bool Exists<TMessage>()
    {
        var items = collection.OfType<EventContainer<TMessage>>();
        return items.Count() > 0;
    }

    public void Invoke<TMessage>(TMessage update, object? sender = null)
    {
        var items = collection.OfType<EventContainer<TMessage>>();
        if (items.Count() > 1)
        {
            throw new ArgumentException("Attempted to access duplicated event type in EventSourceCollection.");
        }
        else if (items.Count() == 1)
        {
            foreach (var item in items)
            {
                item.Invoke(update, sender);
            }
        }
        else
        {
            throw new ArgumentException("Attempted to access non-existent event type in EventSourceCollection.");
        }
    }

    public EventContainer<TMessage> GetEventContainer<TMessage>()
    {
        var items = collection.OfType<EventContainer<TMessage>>();
        if (items.Count() > 1)
        {
            throw new ArgumentException("Attempted to access duplicated event type in EventSourceCollection.");
        }
        else if (items.Count() == 1)
        {
            foreach (var item in items)
            {
                return item;
            }
        }

        throw new ArgumentException("Attempted to access non-existent event type in EventSourceCollection.");
    }

    public EventForwarder<TMessage> GetEventForwarder<TMessage>(ForwardedEventQueue destinationQueue)
    {
        return new EventForwarder<TMessage>(destinationQueue, GetEventContainer<TMessage>());
    }

    public void ForwardFromOtherUsingQueue<TMessage>(EventCollection other, ForwardedEventQueue destinationQueue)
    {
        other.ConnectEventForwarder<TMessage>(GetEventForwarder<TMessage>(destinationQueue));
    }

    public void AddEventAndForwardFromOtherUsingQueue<TMessage>(EventCollection other, ForwardedEventQueue destinationQueue)
    {
        if (Exists<TMessage>())
        {
            throw new ArgumentException("Attempted to add pre-existing event type into EventSourceCollection.");
        }

        if (!other.Exists<TMessage>())
        {
            throw new ArgumentException("Attempted to access non-existent event type in EventSourceCollection.");
        }

        EnableInvokeFor<TMessage>();
        other.ConnectEventForwarder<TMessage>(GetEventForwarder<TMessage>(destinationQueue));
    }

    public void ConnectEventHandler<TMessage>(EventHandler<TMessage> eventHandler)
    {
        var items = collection.OfType<EventContainer<TMessage>>();
        if (items.Count() > 1)
        {
            throw new ArgumentException("Attempted to access duplicated event type in EventSourceCollection.");
        }
        else if (items.Count() == 1)
        {
            foreach (var item in items)
            {
                item.ConnectEventHandler(eventHandler);
            }
        }
        else
        {
            throw new ArgumentException("Attempted to access non-existent event type in EventSourceCollection.");
        }
    }

    public void ConnectEventForwarder<TMessage>(EventForwarder<TMessage> forwarder)
    {
        var items = collection.OfType<EventContainer<TMessage>>();
        if (items.Count() > 1)
        {
            throw new ArgumentException("Attempted to access duplicated event type in EventSourceCollection.");
        }
        else if (items.Count() == 1)
        {
            foreach (var item in items)
            {
                item.ConnectForwarder(forwarder);
            }
        }
        else
        {
            throw new ArgumentException("Attempted to access non-existent event type in EventSourceCollection.");
        }
    }
}
