using System.Threading.Channels;
using OpenRTIC.BasicDevices;

namespace OpenRTIC.Conversation;

public abstract class MessageChannelBase<TMessage>
{
    private readonly Channel<TMessage> _channel = Channel.CreateUnbounded<TMessage>();

    public ChannelReader<TMessage> Reader => _channel.Reader;

    public ChannelWriter<TMessage> Writer => _channel.Writer;

    abstract protected TMessage? GetNull();

    public bool TryWrite(TMessage value)
    {
        return Writer.TryWrite(value);
    }

    public async Task<TMessage?> ReadAsync(CancellationToken cancellation)
    {
        try
        {
            return await Reader.ReadAsync(cancellation);
        }
        catch (Exception ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
            return GetNull();
        }
    }

    public TMessage? Read(CancellationToken cancellation)
    {
        TMessage? result = GetNull();
        Task<TMessage?> asyncValue = ReadAsync(cancellation);
        asyncValue.Wait(cancellation);
        if (asyncValue.IsCompleted)
        {
            result = asyncValue.Result;
        }
        asyncValue.Dispose();
        return result;
    }

    public int ReadAll(Func<TMessage, bool> callback)
    {
        int count = 0;
        try
        {
            bool tryReading = true;
            for (int tryCount = _channel.Reader.Count; tryCount > 0 && tryReading; tryCount = _channel.Reader.Count)
            {
                for (int i = 0; i < tryCount && tryReading; i++)
                {
                    TMessage? item = GetNull();
                    if (_channel.Reader.TryRead(out item))
                    {
                        if (item is not null)
                        {
                            tryReading = callback(item);
                            count++;
                        }
                    }
                    else
                    {
                        tryReading = false;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
        }
        return count;
    }
}
