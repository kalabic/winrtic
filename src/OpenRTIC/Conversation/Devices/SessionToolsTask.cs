using OpenRTIC.BasicDevices;
using OpenRTIC.BasicDevices.RTIC;

namespace OpenRTIC.Conversation.Devices;

public class SessionToolsTask : DeviceTask<ConversationToolsStream>
{
    public SessionToolsTask(ConversationToolsStream device)
        : base(device)
    {
#if DEBUG
        SetLabel("Session Tools");
#endif
    }

    public void StreamingStarted(string itemId, string functionName, string functionCallId, string functionCallArguments)
    {
        DeviceNotifications.Info($" <<< Function started - ItemId:{itemId} Name:{functionName} CallId:{functionCallId} Arg:{functionCallArguments}");
    }

    public void StreamingDelta(string itemId, string functionCallId, string functionArguments) { }

    public void StreamingFinished(string itemId, string functionName, string functionCallId, string functionCallArguments)
    {
        DeviceNotifications.Info($" <<< Function finished - ItemId:{itemId} Name:{functionName} CallId:{functionCallId} Arg:{functionCallArguments}");
    }

    protected override void TaskFunction(CancellationToken cancellation)
    {
    }
}
