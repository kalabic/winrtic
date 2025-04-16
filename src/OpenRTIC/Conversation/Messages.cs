using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation;

public class ConsoleMessage 
{
    public enum SessionEvent
    {
        SessionStarted,
        MicrophoneStarted
    }

    public enum ItemEvent
    {
        StreamingStarted,
        StreamingFinished
    }
}

public class ConsoleMessage_SessionEvent : ConsoleMessage
{
    public readonly SessionEvent Event;

    public ConsoleMessage_SessionEvent(SessionEvent ev)
    {
        this.Event = ev;
    }
}

public class ConsoleMessage_ItemEvent : ConsoleMessage
{
    public readonly ItemEvent Event;

    public ConsoleMessage_ItemEvent(ItemEvent ev)
    {
        this.Event = ev;
    }
}

public class ConsoleMessage_StreamText : ConsoleMessage
{
    public readonly ItemAttributes Attrib;
    public readonly string Value;

    public ConsoleMessage_StreamText(ItemAttributes attrib, string text)
    {
        this.Attrib = new(attrib);
        this.Value = text;
    }
}

public class ConsoleChannel : MessageChannelBase<ConsoleMessage>
{
    override protected ConsoleMessage? GetNull() { return null; }
}
