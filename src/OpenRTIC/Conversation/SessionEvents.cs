namespace OpenRTIC.Conversation;

public class ConversationSessionException
{
    public readonly Exception Exception;

    public ConversationSessionException(Exception ex)
    {
        this.Exception = ex;
    }
}

public class SendAudioTaskFinished
{
    public SendAudioTaskFinished() { }
}

public class InputAudioTaskFinished
{
    public InputAudioTaskFinished() { }
}
