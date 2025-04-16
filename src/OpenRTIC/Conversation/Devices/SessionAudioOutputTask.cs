using OpenRTIC.MiniTaskLib;
using OpenRTIC.BasicDevices;

namespace OpenRTIC.Conversation.Devices;

public class PlaybackFinishedUpdate
{
    public const PlaybackFinishedUpdate? Default = null;

    private ItemAttributes enqueuedItem;

    public PlaybackFinishedUpdate(ItemAttributes enqueuedItem)
    {
        this.enqueuedItem = enqueuedItem;
    }
}

public class PlaybackPositionReachedUpdate
{
    public const PlaybackPositionReachedUpdate? Default = null;

    public string ItemId { get { return _itemAttrib.ItemId; } }

    public ItemAttributes ItemAttrib { get { return _itemAttrib; } }

    private ItemAttributes _itemAttrib;

    public PlaybackPositionReachedUpdate(ItemAttributes itemAttrib)
    {
        this._itemAttrib = new ItemAttributes(itemAttrib);
    }
}


public class SessionAudioOutputTask : DeviceTask<CircularBufferStreamBase>
{
    public EventCollection outputAudioUpdates = new();

    protected SemaphoreSlim itemsSemaphore = new SemaphoreSlim(1, 1);

    // Used for raising 'PlaybackPositionReached' event.
    private ItemAttributes enqueuedItem = new ItemAttributes();

    // Used for raising 'PlaybackPositionReached' event.
    private ItemAttributes markedItem = new ItemAttributes();

    private long markedPositionMiliseconds = -1;

    public SessionAudioOutputTask(CircularBufferStreamBase device)
        : base(device)
    {
        outputAudioUpdates.EnableInvokeFor<PlaybackFinishedUpdate>();
        outputAudioUpdates.EnableInvokeFor<PlaybackPositionReachedUpdate>();
#if DEBUG
        SetLabel("Session Audio Output");
#endif
    }

    public void NotifyItemCleared(ItemAttributes item)
    {
    }

    public void NotifyItemEnqueued(ItemAttributes item)
    {
        itemsSemaphore.Wait();
        enqueuedItem.Set(item);
        itemsSemaphore.Release();
    }

    public void SetVolume(float value)
    {
        if (Device is SpeakerAudioStream speakerAudio)
        {
            speakerAudio.Volume = value;
        }
    }

    public void SetPlaybackPositionReachedNotification(long milisecondsDelay, ItemAttributes item)
    {
        long newMarkedPosition = Device.Position + milisecondsDelay;
        itemsSemaphore.Wait();
        markedItem.Set(item);
        markedPositionMiliseconds = newMarkedPosition;
        itemsSemaphore.Release();
    }

    public void ResetPlaybackPositionReachedNotification()
    {
        itemsSemaphore.Wait();
        markedItem.Clear();
        markedPositionMiliseconds = -1;
        itemsSemaphore.Release();
    }

    protected override void TaskFunction(CancellationToken cancellation)
    {
        long previousPlaybackState = 0;
        while (!Device.IsCancellationRequested() && !cancellation.IsCancellationRequested)
        {
            //
            // Logic for invoking SpeakerPlaybackFinished event.
            // (TODO: This is incorrect approach, fix it later)
            //
            long playbackState = Device.Length;
            if (playbackState != previousPlaybackState)
            {
                if (playbackState == 0)
                {
                    outputAudioUpdates?.Invoke(new PlaybackFinishedUpdate(enqueuedItem));
                }

                previousPlaybackState = playbackState;
            }

            //
            // Logic for invoking PlaybackPositionReached event.
            //
            PlaybackPositionReachedUpdate? playbackPositionReached = null;
            itemsSemaphore.Wait();
            if ((markedPositionMiliseconds > 0) && (markedItem.LocalId > 0))
            {
                long currentPosition = Device.Position;
                if ((currentPosition > markedPositionMiliseconds) && (enqueuedItem.LocalId == markedItem.LocalId))
                {
                    playbackPositionReached = new PlaybackPositionReachedUpdate(markedItem);
                    markedItem.Clear();
                    markedPositionMiliseconds = -1;
                }
            }
            itemsSemaphore.Release();
            if (playbackPositionReached is not null)
            {
                outputAudioUpdates?.Invoke(playbackPositionReached);
            }

            Thread.Sleep(250);
        }
    }
}
