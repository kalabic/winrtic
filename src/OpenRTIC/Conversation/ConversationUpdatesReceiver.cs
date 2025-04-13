using OpenAI.RealtimeConversation;
using System.Net.WebSockets;
using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002

public class ConversationUpdatesReceiver : ConversationUpdatesDispatcher
{
    public ConversationReceiverState ReceiverState { get { return _sessionState.receiverState; } }

    public EventCollection ReceiverEvents { get { return _receiverEvents; } }

    public ConversationUpdatesInfo SessionState { get { return _sessionState; } }

    public ConversationCancellation Cancellation { get { return _cancellation; } }

    public bool IsWebSocketOpen { get { return (_session is not null) ? (_session.WebSocket.State == WebSocketState.Open) : false; } }


    protected ConversationCancellation _cancellation;

    protected EventCollection _receiverEvents = new();

    protected RealtimeConversationSession? _session = null;


    public ConversationUpdatesReceiver()
        : this(CancellationToken.None) { }

    public ConversationUpdatesReceiver(CancellationToken cancellation)
    {
        this._cancellation = new ConversationCancellation(cancellation);

        // Register events collection in this class to be invoked from base receiver's forwarded event queue task.
        base.ForwardToOtherUsingQueue(_receiverEvents);

        _receiverEvents.EnableInvokeFor<InputAudioTaskFinished>();
        _receiverEvents.EnableInvokeFor<FailedToConnect>();
    }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            _receiverEvents.Clear();
            _session?.Dispose();
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    public void Run()
    {
        MessageQueueEntry(CancellationToken.None);
        // TODO: Check that receiver was in fact properly finished.
    }

    public void RunAsync()
    {
        MessageQueueEntryAsync(CancellationToken.None);
    }

    public void SetSession(RealtimeConversationSession session)
    {
        this._session = session;
    }

    public void CancelMicrophone()
    {
        _cancellation.CancelMicrophone();
    }

    public void AudioInputFinished()
    {
        _receiverEvents.Invoke<InputAudioTaskFinished>(new InputAudioTaskFinished());
    }

    public void FailedToConnect(string message)
    {
        _receiverEvents.Invoke<FailedToConnect>(new FailedToConnect(message));
    }

    public void CloseMessageQueue()
    {
        TaskEvents.Invoke<CloseMessageQueue>(new CloseMessageQueue());
    }

    public void InterruptResponse()
    {
        HandleSessionExceptions(() =>
        {
            _session?.InterruptResponseAsync();
        });
    }

    public void FinishReceiver()
    {
        if (_sessionState.receiverState == ConversationReceiverState.Connected)
        {
            _sessionState.receiverState = ConversationReceiverState.FinishAfterResponse;
            HandleSessionExceptions(() =>
            {
                _session?.InterruptResponseAsync();
            });
        }
    }

    public void SendAudioInput(Stream audioStream, CancellationToken cancellation)
    {
        HandleSessionExceptions( () =>
        {
            if (IsWebSocketOpen)
            {
                _session?.SendInputAudio(audioStream, cancellation);
            }
        });

        HandleSessionExceptions( () =>
        {
            if (IsWebSocketOpen)
            {
                _session?.ClearInputAudio();
            }
        });
    }

    public async Task SendAudioInputAsync(Stream audioStream, CancellationToken cancellation)
    {
        await HandleSessionExceptionsAsync(async () =>
        {
            if ((_session is not null) && IsWebSocketOpen)
            {
                await _session.SendInputAudioAsync(audioStream, cancellation);
            }
        });

        await HandleSessionExceptionsAsync(async () =>
        {
            if ((_session is not null) && IsWebSocketOpen)
            {
                await _session.ClearInputAudioAsync();
            }
        });
    }

    public void ReceiveUpdates(CancellationToken cancellation)
    {
        var task = HandleSessionExceptionsAsync(async () =>
        {
            if (_session is not null)
            {
                _sessionState.receiverState = ConversationReceiverState.Connected;
                await foreach (ConversationUpdate update in _session.ReceiveUpdatesAsync(_cancellation.WebSocketToken))
                {
                    if (!DispatchAndProcess(update))
                    {
                        break;
                    }
                }
                _sessionState.receiverState = ConversationReceiverState.Disconnected;
            }
        });

        HandleSessionExceptions( () => task.Wait() );
    }

    protected async Task ReceiveUpdatesAsync()
    {
        _sessionState.receiverState = ConversationReceiverState.Connected;
        await HandleSessionExceptionsAsync(async () =>
        {
            if (_session is not null)
            {
                await foreach (ConversationUpdate update in _session.ReceiveUpdatesAsync(_cancellation.WebSocketToken))
                {
                    if (!DispatchAndProcess(update))
                    {
                        break;
                    }
                }
            }
        });
        _sessionState.receiverState = ConversationReceiverState.Disconnected;
    }

    private bool DispatchAndProcess(ConversationUpdate update)
    {
        DispatchUpdate(update);

        // Normal state, continue receiving updates as usual.
        if (_sessionState.receiverState == ConversationReceiverState.Connected)
        {
            return true;
        }

        if (_sessionState.receiverState == ConversationReceiverState.FinishAfterResponse)
        {
            if (_sessionState.ResponseStarted)
            {
                return true;
            }

            _sessionState.receiverState = ConversationReceiverState.Disconnecting;
        }

        if (_sessionState.receiverState == ConversationReceiverState.Disconnecting)
        {
            if (IsWebSocketOpen)
            {
                HandleSessionExceptions(() =>
                {
                    var socket = _session?.WebSocket;
                    if (socket != null)
                    {
                        _ = socket.CloseOutputAsync(
                            WebSocketCloseStatus.NormalClosure, null, _cancellation.WebSocketToken);
                    }
                });
                return true;
            }
            else
            {
                // If socket is closed, return false to break the receiver loop.
                return false;
            }
        }

        return true;
    }
}
