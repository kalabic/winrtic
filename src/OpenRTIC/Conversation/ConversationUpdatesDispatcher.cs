using OpenAI.RealtimeConversation;
using System.Net.WebSockets;
using OpenRTIC.BasicDevices;
using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002


/// <summary>
/// This class starts additional task to invoke 'forwarded event handlers' that do not
/// hang on network updates fetcher task when it invokes event with an update.
/// </summary>
public abstract class ConversationUpdatesDispatcher : ForwardedEventQueue
{
    protected ConversationUpdatesInfo _sessionState = new();

    protected ConversationUpdatesDispatcher(CancellationToken? cancellation = null)
        : base(cancellation)
    {
        // Adding standard 'EventHandler' for an event from this 'TaskEvents' collection will not
        // automatically invoke them from 'ForwardedEventQueue' task, for that to happen
        // it is necessary to register forwarded event handlers (done in ConversationUpdatesReceiver)
        EventCollection conversationUpdates = TaskEvents;

        // All events are C# generics, any type can be enabled for 'Invoke' operation.
        conversationUpdates.EnableInvokeFor<ConversationSessionException>();
        conversationUpdates.EnableInvokeFor<ConversationSessionStartedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationInputAudioClearedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationInputAudioCommittedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemCreatedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemDeletedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationErrorUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationInputSpeechStartedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationInputSpeechFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingAudioFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationInputTranscriptionFailedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationInputTranscriptionFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingAudioTranscriptionFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingPartDeltaUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingPartFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingStartedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemStreamingTextFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationRateLimitsUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationResponseFinishedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationResponseStartedUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationSessionConfiguredUpdate>();
        conversationUpdates.EnableInvokeFor<ConversationItemTruncatedUpdate>();
    }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            _sessionState.Disposed = true;
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    protected void HandleSessionExceptions(Action sessionFunction)
    {
        try
        {
            sessionFunction();
        }
        catch (WebSocketException ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
            TaskEvents.Invoke(new ConversationSessionException(ex));
        }
        catch (OperationCanceledException ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
            TaskEvents.Invoke(new ConversationSessionException(ex));
        }
    }

    protected async Task HandleSessionExceptionsAsync(Func<Task> sessionFunctionAsync)
    {
        try
        {
            await sessionFunctionAsync();
        }
        catch (WebSocketException ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
            TaskEvents.Invoke(new ConversationSessionException(ex));
        }
        catch (OperationCanceledException ex)
        {
            DeviceNotifications.ExceptionOccured(ex);
            TaskEvents.Invoke(new ConversationSessionException(ex));
        }
    }

    protected void DispatchUpdate(ConversationUpdate update)
    {
        if (update is ConversationErrorUpdate errorUpdate)
        {
            TaskEvents.Invoke(errorUpdate);
        }
        else if (update is ConversationInputAudioClearedUpdate audioClearedUpdate)
        {
            _sessionState.nInputAudioCleared++;
            _sessionState.SpeechStarted = false;
            _sessionState.WaitingTranscription = false;
            TaskEvents.Invoke(audioClearedUpdate);
        }
        else if (update is ConversationInputAudioCommittedUpdate audioCommitedUpdate)
        {
            TaskEvents.Invoke(audioCommitedUpdate);
        }
        else if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
        {
            _sessionState.SpeechStarted = true;
            TaskEvents.Invoke(speechStartedUpdate);
        }
        else if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
        {
            _sessionState.SpeechStarted = false;
            _sessionState.WaitingTranscription = true;
            TaskEvents.Invoke(speechFinishedUpdate);
        }
        else if (update is ConversationInputTranscriptionFailedUpdate transcriptionFailedUpdate)
        {
            _sessionState.nTranscriptionFailed++;
            _sessionState.WaitingTranscription = false;
            TaskEvents.Invoke(transcriptionFailedUpdate);
        }
        else if (update is ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate)
        {
            _sessionState.nTranscriptionFinished++;
            _sessionState.WaitingTranscription = false;
            TaskEvents.Invoke(transcriptionFinishedUpdate);
        }
        else if (update is ConversationItemCreatedUpdate itemCreatedUpdate)
        {
            TaskEvents.Invoke(itemCreatedUpdate);
        }
        else if (update is ConversationItemDeletedUpdate itemDeletedUpdate)
        {
            TaskEvents.Invoke(itemDeletedUpdate);
        }
        else if (update is ConversationItemStreamingAudioFinishedUpdate audioFinishedUpdate)
        {
            TaskEvents.Invoke(audioFinishedUpdate);
        }
        else if (update is ConversationItemStreamingAudioTranscriptionFinishedUpdate audioTranscriptionFinishedUpdate)
        {
            TaskEvents.Invoke(audioTranscriptionFinishedUpdate);
        }
        else if (update is ConversationItemStreamingFinishedUpdate streamingFinishedUpdate)
        {
            _sessionState.StreamingStarted = false;
            TaskEvents.Invoke(streamingFinishedUpdate);
        }
        else if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
        {
            TaskEvents.Invoke(deltaUpdate);
        }
        else if (update is ConversationItemStreamingPartFinishedUpdate partFinishedUpdate)
        {
            TaskEvents.Invoke(partFinishedUpdate);
        }
        else if (update is ConversationItemStreamingStartedUpdate streamingStartedUpdate)
        {
            _sessionState.StreamingStarted = true;
            TaskEvents.Invoke(streamingStartedUpdate);
        }
        else if (update is ConversationItemStreamingTextFinishedUpdate textFinishedUpdate)
        {
            TaskEvents.Invoke(textFinishedUpdate);
        }
        else if (update is ConversationItemTruncatedUpdate truncatedUpdate)
        {
            TaskEvents.Invoke(truncatedUpdate);
        }
        else if (update is ConversationRateLimitsUpdate rateLimitsUpdate)
        {
            TaskEvents.Invoke(rateLimitsUpdate);
        }
        else if (update is ConversationResponseFinishedUpdate responseFinishedUpdate)
        {
            _sessionState.nResponseFinished++;
            _sessionState.ResponseStarted = false;
            TaskEvents.Invoke(responseFinishedUpdate);
        }
        else if (update is ConversationResponseStartedUpdate responseStartedUpdate)
        {
            _sessionState.nResponseStarted++;
            _sessionState.ResponseStarted = true;
            TaskEvents.Invoke(responseStartedUpdate);
        }
        else if (update is ConversationSessionConfiguredUpdate sessionConfiguredUpdate)
        {
            TaskEvents.Invoke(sessionConfiguredUpdate);
        }
        else if (update is ConversationSessionStartedUpdate sessionStartedUpdate)
        {
            _sessionState.SessionStarted = true;
            TaskEvents.Invoke(sessionStartedUpdate);
        }
    }
}
