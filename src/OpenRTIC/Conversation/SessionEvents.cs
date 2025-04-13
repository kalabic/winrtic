namespace OpenRTIC.Conversation;

public class ConversationSessionException
{
    public readonly Exception Exception;

    public ConversationSessionException(Exception ex)
    {
        this.Exception = ex;
    }
}

public class FailedToConnect
{
    public readonly string _message;

    public FailedToConnect(string message) 
    {
        this._message = message;
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
