using OpenRTIC.BasicDevices;

namespace OpenRTIC.Conversation.Devices;

public class ConversationToolsStream : CircularBufferStreamBase
{
    private const int DEFAULT_BUFFER_SIZE = 64 * 1024;

    public ConversationToolsStream(CancellationToken cancellation)
        : base(DEFAULT_BUFFER_SIZE, cancellation)
    { }

    public ConversationToolsStream()
        : base(DEFAULT_BUFFER_SIZE)
    { }
}
