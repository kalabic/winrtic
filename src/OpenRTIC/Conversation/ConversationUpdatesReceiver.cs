using OpenAI.RealtimeConversation;
using System.Net.WebSockets;
using OpenRTIC.BasicDevices;
using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002

public class ConversationUpdatesReceiver : ConversationUpdatesDispatcher
{
    protected const int SAMPLES_PER_SECOND = 24000;
    protected const int BYTES_PER_SAMPLE = 2;
    protected const int CHANNELS = 1;
    protected const int AUDIO_INPUT_BUFFER_SECONDS = 2;

    public static readonly AudioStreamFormat AudioFormat = new(SAMPLES_PER_SECOND, CHANNELS, BYTES_PER_SAMPLE);

    public ConversationReceiverState ReceiverState { get { return _sessionState.receiverState; } }

    public EventCollection ReceiverEvents { get { return _receiverEvents; } }

    public ConversationUpdatesInfo SessionState { get { return _sessionState; } }

    public ConversationCancellation Cancellation { get { return _cancellation; } }


    protected ConversationCancellation _cancellation;

    protected EventCollection _receiverEvents = new();

    protected RealtimeConversationSession _session;


    public ConversationUpdatesReceiver(RealtimeConversationSession session,
                                       CancellationToken? cancellation = null)
    {
        this._cancellation = new ConversationCancellation(cancellation);
        this._session = session;
        EventCollection conversationUpdates = TaskEvents;
        _receiverEvents.MakeCompatible(conversationUpdates);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationSessionStartedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationInputAudioClearedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationInputAudioCommittedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemCreatedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemDeletedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationErrorUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationInputSpeechStartedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationInputSpeechFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingAudioFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationInputTranscriptionFailedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationInputTranscriptionFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingAudioTranscriptionFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingPartDeltaUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingPartFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingStartedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemStreamingTextFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationRateLimitsUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationResponseFinishedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationResponseStartedUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationSessionConfiguredUpdate>(conversationUpdates, this);
        _receiverEvents.ForwardFromOtherUsingQueue<ConversationItemTruncatedUpdate>(conversationUpdates, this);

        // Start 'forwarded event queue'.
        Start();
    }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            _receiverEvents.Clear();
            _session.Dispose();
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    public void InterruptResponse()
    {
        HandleSessionExceptions(() =>
        {
            _session.InterruptResponseAsync();
        });
    }

    public void FinishReceiver()
    {
        if (_sessionState.receiverState == ConversationReceiverState.Connected)
        {
            _sessionState.receiverState = ConversationReceiverState.FinishAfterResponse;
            HandleSessionExceptions(() =>
            {
                _session.InterruptResponseAsync();
            });
        }
    }

    public void SendAudioInput(Stream audioStream, CancellationToken cancellation)
    {
        HandleSessionExceptions( () =>
        {
            if (_session.WebSocket.State == WebSocketState.Open)
            {
                _session.SendInputAudio(audioStream, cancellation);
            }
        });

        HandleSessionExceptions( () =>
        {
            if (_session.WebSocket.State == WebSocketState.Open)
            {
                _session.ClearInputAudio();
            }
        });
    }

    public async Task SendAudioInputAsync(Stream audioStream, CancellationToken cancellation)
    {
        await HandleSessionExceptionsAsync(async () =>
        {
            if (_session.WebSocket.State == WebSocketState.Open)
            {
                await _session.SendInputAudioAsync(audioStream, cancellation);
            }
        });

        await HandleSessionExceptionsAsync(async () =>
        {
            if (_session.WebSocket.State == WebSocketState.Open)
            {
                await _session.ClearInputAudioAsync();
            }
        });
    }

    protected void ReceiveUpdates(CancellationToken cancellation)
    {
        HandleSessionExceptionsAsync( async () =>
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
        }).Wait(cancellation);
    }

    protected async Task ReceiveUpdatesAsync()
    {
        _sessionState.receiverState = ConversationReceiverState.Connected;
        await HandleSessionExceptionsAsync(async () =>
        {
            await foreach (ConversationUpdate update in _session.ReceiveUpdatesAsync(_cancellation.WebSocketToken))
            {
                if (!DispatchAndProcess(update))
                {
                    break;
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
            WebSocket socket = _session.WebSocket;
            if (socket.State == WebSocketState.Open)
            {
                HandleSessionExceptions(() =>
                {
                    _ = socket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure, null, _cancellation.WebSocketToken);
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
