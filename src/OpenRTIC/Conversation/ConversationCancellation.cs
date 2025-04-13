namespace OpenRTIC.Conversation;

public class ConversationCancellation
{
    public ConversationCancellation()
    {
        _shellCanceler = new CancellationTokenSource();
        _speechCanceler = new CancellationTokenSource();
        _microphoneCanceler = CancellationTokenSource.CreateLinkedTokenSource(_shellCanceler.Token);
        _webSocketCanceler = new CancellationTokenSource();
    }

    public ConversationCancellation(CancellationToken externalToken)
    {
        _shellCanceler = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        _speechCanceler = new CancellationTokenSource();
        _microphoneCanceler = CancellationTokenSource.CreateLinkedTokenSource(_shellCanceler.Token);
        _webSocketCanceler = new CancellationTokenSource();
    }

    public bool IsMicrophoneCancelled { get { return _microphoneCanceler.IsCancellationRequested; } }

    public void CancelMicrophone()
    {
        _microphoneCanceler.Cancel();
    }


    public CancellationToken ShellToken { get { return _shellCanceler.Token; } }

    public CancellationToken SpeechToken { get { return _speechCanceler.Token; } }

    public CancellationToken MicrophoneToken { get { return _microphoneCanceler.Token; } }

    public CancellationToken WebSocketToken { get { return _webSocketCanceler.Token; } }



    protected CancellationTokenSource _shellCanceler;

    protected CancellationTokenSource _speechCanceler;

    protected CancellationTokenSource _microphoneCanceler;

    protected CancellationTokenSource _webSocketCanceler;
}
