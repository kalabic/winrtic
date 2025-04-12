namespace OpenRTIC.Conversation;

public class ConversationCancellation
{
    public ConversationCancellation(CancellationToken? externalToken = null)
    {
        if (externalToken is not null)
        {
            shellCanceler = CancellationTokenSource.CreateLinkedTokenSource((CancellationToken)externalToken);
        }
        else
        {
            shellCanceler = new CancellationTokenSource();
        }
        speechCanceler = new CancellationTokenSource();
        microphoneCanceler = CancellationTokenSource.CreateLinkedTokenSource(shellCanceler.Token);
        webSocketCanceler = new CancellationTokenSource();
    }



    public CancellationToken ShellToken { get { return shellCanceler.Token; } }

    public CancellationToken SpeechToken { get { return speechCanceler.Token; } }

    public CancellationToken MicrophoneToken { get { return microphoneCanceler.Token; } }

    public CancellationToken WebSocketToken { get { return webSocketCanceler.Token; } }



    protected CancellationTokenSource shellCanceler;

    protected CancellationTokenSource speechCanceler;

    protected CancellationTokenSource microphoneCanceler;

    protected CancellationTokenSource webSocketCanceler;
}
