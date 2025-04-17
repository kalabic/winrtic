using OpenAI.RealtimeConversation;
using OpenRTIC.BasicDevices.RTIC;
using OpenRTIC.Config;
using OpenRTIC.Conversation.Devices;
using OpenRTIC.MiniTaskLib;
using System.Diagnostics;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002

/// <summary>
/// WIP
/// </summary>
public partial class ConversationShell : IDisposable
{
    private ConversationUpdatesReceiverTask _updatesReceiver;

    /// <summary>
    /// Temporary local storage for items from conversation stream. WIP.
    /// </summary>
    private readonly Dictionary<string, ConversationStreamItem> _streamItemMap = new Dictionary<string, ConversationStreamItem>();

    private readonly IConversationDevices _devices;

    private readonly Stopwatch _speechWatch = new Stopwatch();

    private int _nextLocalItemId = 1;

    private int getNextLocalItemId()
    {
        int id = _nextLocalItemId++;
        return id;
    }

    protected ConversationShell(IRTIConsole console, RealtimeConversationClient client)
        : this(console, client, CancellationToken.None)
    { }

    protected ConversationShell(IRTIConsole console,
                                RealtimeConversationClient client,
                                CancellationToken cancellation)
    {
        _devices = ConversationDevices.Start(console, cancellation);
        _updatesReceiver = new ConversationUpdatesReceiverTask(cancellation);
        _updatesReceiver.ConfigureWith(client, _devices.GetAudioInput());

        ConnectDeviceEventHandlers();
        ConnectConversationUpdateHandlers();
    }

    protected ConversationShell(IRTIConsole console,
                                ConversationOptions options,
                                CancellationToken cancellation)
    {
        _devices = ConversationDevices.Start(console, cancellation);
        _updatesReceiver = new ConversationUpdatesReceiverTask(cancellation);
        _updatesReceiver.ConfigureWith(options, _devices.GetAudioInput());

        ConnectDeviceEventHandlers();
        ConnectConversationUpdateHandlers();
    }

    virtual public void Dispose()
    {
        _updatesReceiver.Dispose();
    }

    public void ReceiveUpdates()
    {
        _updatesReceiver.Run();
    }

    public void ReceiveUpdatesAsync()
    {
        _updatesReceiver.RunAsync();
    }

    public Task? GetAwaiter()
    {
        return _updatesReceiver.GetAwaiter();
    }

    private EventForwarder<TMessage> NewQueuedEventForwarder<TMessage>(EventHandler<TMessage> eventHandler)
    {
        // Make this event arrive from a dedicated task in updates receiver.
        return _updatesReceiver.Queue.NewQueuedEventForwarder<TMessage>(eventHandler);
    }

    private void ConnectDeviceEventHandlers()
    {
        _devices.ConnectReceiverEvents(_updatesReceiver.ReceiverEvents);
        _devices.ConnectSessionEvents(_updatesReceiver.ReceiverQueueEvents);


        //
        // PlaybackPositionReachedUpdate
        //
        var audioOutputSessionUpdatesSource = _devices.GetSessionAudioOutputUpdates();
        audioOutputSessionUpdatesSource.Connect(
            NewQueuedEventForwarder<PlaybackPositionReachedUpdate>((_, update) =>
        {
            if (_streamItemMap.ContainsKey(update.ItemId))
            {
                _streamItemMap.Remove(update.ItemId);
            }
            _devices.ClearPlayback(update.ItemAttrib);
        }));
    }

    private void ConnectConversationUpdateHandlers()
    {
        var receiverEvents = _updatesReceiver.ReceiverEvents;

        //
        // ConversationInputSpeechStartedUpdate
        //
        receiverEvents.Connect<ConversationInputSpeechStartedUpdate>(false, (_, update) =>
        {
            _speechWatch.Start();
        });

        //
        // ConversationInputSpeechFinishedUpdate
        //
        receiverEvents.Connect<ConversationInputSpeechFinishedUpdate>(false, (_, update) =>
        {
            _speechWatch.Stop();
        });

        //
        // ConversationItemStreamingStartedUpdate
        //
        receiverEvents.Connect<ConversationItemStreamingStartedUpdate>(false, (_, update) =>
        {
            ConversationStreamItem streamItem = new ConversationStreamItem(update.ItemId, getNextLocalItemId(), update.FunctionName);
            _streamItemMap.Add(streamItem.Attrib.ItemId, streamItem);
        });

        //
        // ConversationItemStreamingFinishedUpdate
        //
        receiverEvents.Connect<ConversationItemStreamingFinishedUpdate>(false, (_, update) =>
        {
            if (_streamItemMap.ContainsKey(update.ItemId))
            {
                ConversationStreamItem item = _streamItemMap[update.ItemId];
                _streamItemMap.Remove(update.ItemId);
            }
        });

        //
        // ConversationItemStreamingPartDeltaUpdate
        //
        receiverEvents.Connect<ConversationItemStreamingPartDeltaUpdate>(false, (_, update) =>
        {
            if (_streamItemMap.ContainsKey(update.ItemId))
            {
                var item = _streamItemMap[update.ItemId];
                if (update.AudioBytes is not null)
                {
                    _devices.EnqueueForPlayback(item.Attrib, update.AudioBytes);
                }
            }
#if DEBUG
            else
            {
                throw new InvalidOperationException("Not nice");
            }
#endif
        });
    }

    /// <summary>
    /// WIP
    /// </summary>
    /// <param name="timeoutMs"></param>
    /// <returns></returns>
    public long FinishSession(int timeoutMs = -1)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Receiver has its own timeout for cancelling, so not really needed here.
        _updatesReceiver.Cancel();
        var awaiter = _updatesReceiver.GetAwaiter();
        if (awaiter is not null && !awaiter.IsCompleted)
        {
            awaiter.Wait(timeoutMs);
        }
        stopwatch.Stop();

        // Devices too have a cancel timeout.
        long finishDevicesMs = _devices.CancelStopDisposeAll();

        // Maybe of interest, so return total elapsed cancelling time.
        return stopwatch.ElapsedMilliseconds + finishDevicesMs;
    }
}
