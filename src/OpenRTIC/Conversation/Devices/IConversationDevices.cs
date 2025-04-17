using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation.Devices;

public interface IConversationDevices
{
    public Stream GetAudioInput();

    public void ConnectReceiverEvents(EventCollection receiverEvents);

    public void ConnectSessionEvents(EventCollection sessionEvents);

    public EventCollection GetSessionAudioOutputUpdates();

    public bool ClearPlayback(ItemAttributes item);

    public long CancelStopDisposeAll();

    public void EnqueueForPlayback(ItemAttributes item, BinaryData audioData);
}
