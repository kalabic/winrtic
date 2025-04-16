using OpenAI.RealtimeConversation;
using OpenRTIC.MiniTaskLib;
using OpenRTIC.BasicDevices;
using OpenRTIC.Config;
using OpenRTIC.BasicDevices.RTIC;

namespace OpenRTIC.Conversation.Devices;

#pragma warning disable OPENAI002

/// <summary>
/// A container for all system devices needed for conversation with an AI agent during the session.
/// </summary>
public class ConversationDevices
{
    public const int CLEAR_ENQUEUED_TIMEOUT = 2000; // Miliseconds

    public const float VOLUME_RATIO_DURING_SPEECH = 0.3f;

    private const int STOP_TASK_TIMEOUT = 10000;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="microphoneCancellationToken"></param>
    /// <returns></returns>
    static public ConversationDevices Start(RTIConsole console, CancellationToken microphoneCancellationToken)
    {
        SpeakerAudioStream speakerDevice = new SpeakerAudioStream(ConversationSessionConfig.AudioFormat);
        SessionAudioOutputTask speaker = new SessionAudioOutputTask(speakerDevice);
        MicrophoneAudioStream microphoneDevice = new MicrophoneAudioStream(ConversationSessionConfig.AudioFormat, microphoneCancellationToken);
        SessionConsole sessionConsole = new SessionConsole(console);
        speaker.Start();
        // _microphone will be started after server confirms session was created.
        return new ConversationDevices(speaker, microphoneDevice, sessionConsole);
    }

    public Stream MicrophoneStream { get { return _microphone; } }

    /// <summary>
    /// This is a task that is observing speaker device and generating events interesting for conversation session.
    /// </summary>
    private readonly SessionAudioOutputTask _audioOutputTask;

    private readonly MicrophoneAudioStream _microphone;

    private readonly SessionConsole _console;

    private readonly float _speakerVolumeNormal = 1.0f;

    // Chunks of audio item that was 'cleared' cannot be enqueued again (audio chunks come from stream delta updates).
    private ItemAttributes _clearedItem = new ItemAttributes();

    // Only accept chunks of currently enqueued item.
    private ItemAttributes _enqueuedItem = new ItemAttributes();

    private string _transcriptBuffer = "";

    protected ConversationDevices(SessionAudioOutputTask speaker,
                                  MicrophoneAudioStream microphone,
                                  SessionConsole console)
    {
        _audioOutputTask = speaker;
        _microphone = microphone;
        _console = console;
    }

    public void ConnectReceiverEvents(EventCollection receiverEvents)
    {
        //
        // FailedToConnect
        //
        receiverEvents.Connect<FailedToConnect>((_, update) =>
        {
            _console.WriteError(update._message);
        });
    }

    public void ConnectSessionEvents(EventCollection sessionEvents)
    {
        _console.ConnectSessionEvents(sessionEvents);

        //
        // ConversationInputSpeechStartedUpdate
        //
        sessionEvents.Connect<ConversationInputSpeechStartedUpdate>(false, (_, update) =>
        {
            // Speaker volume is first ratioed and after a timeout playback will be completely cleared.
            SetPlaybackPositionReachedNotification(CLEAR_ENQUEUED_TIMEOUT);
            AdjustSpeakerVolume(VOLUME_RATIO_DURING_SPEECH);
        });

        //
        // ConversationInputSpeechFinishedUpdate
        //
        sessionEvents.Connect<ConversationInputSpeechFinishedUpdate>(false, (_, update) =>
        {
            ResetSpeakerVolume();
            ResetPlaybackPositionReachedNotification();
        });
    }

    public EventCollection GetSessionAudioOutputTaskUpdates()
    {
        return _audioOutputTask.TaskEvents;
    }

    public EventCollection GetSessionAudioOutputUpdates()
    {
        return _audioOutputTask.outputAudioUpdates;
    }

    public List<TaskWithEvents> GetTaskList()
    {
        List<TaskWithEvents> list = new();
        list.Add(_audioOutputTask);
        return list;
    }

    public void HandleEvent_ItemStreamingDelta(ItemAttributes item, ConversationItemStreamingPartDeltaUpdate update)
    {
        if (update.AudioBytes is not null)
        {
            EnqueueForPlayback(item, update.AudioBytes);
        }

        if (!String.IsNullOrEmpty(update.AudioTranscript))
        {
            WriteToConsole(item, update.AudioTranscript);
        }
    }

    public void ResetSpeakerVolume()
    {
        _audioOutputTask.SetVolume(_speakerVolumeNormal);
    }

    public void AdjustSpeakerVolume(float ratio)
    {
        _audioOutputTask.SetVolume(_speakerVolumeNormal * ratio);
    }

    public long GetBufferedMs()
    {
        return _audioOutputTask.GetDevice().Length;
    }

    public string GetTranscriptionBuffer()
    {
        return _transcriptBuffer;
    }

    public void SetPlaybackPositionReachedNotification(long milisecondsDelay)
    {
        _audioOutputTask.SetPlaybackPositionReachedNotification(milisecondsDelay, _enqueuedItem);
    }

    public void ResetPlaybackPositionReachedNotification()
    {
        _audioOutputTask.ResetPlaybackPositionReachedNotification();
    }

    public bool ClearPlayback(ItemAttributes item)
    {
        bool itemCleared = (_clearedItem.LocalId != item.LocalId) && (_enqueuedItem.LocalId == item.LocalId);
        if (itemCleared)
        {
            _clearedItem.Set(_enqueuedItem);
            _audioOutputTask.GetDevice().ClearBuffer();
            ResetPlaybackPositionReachedNotification();
            _audioOutputTask.NotifyItemCleared(_clearedItem);
        }
        return itemCleared;
    }

    public void ClearPlayback()
    {
        // Once enqueued item is 'cleared', do not accept chunks (stream delta updates) with the same id.
        _clearedItem.Set(_enqueuedItem);
        _audioOutputTask.GetDevice().ClearBuffer();
        ResetPlaybackPositionReachedNotification();
        _audioOutputTask.NotifyItemCleared(_clearedItem);
    }

    public void EnqueueForPlayback(ItemAttributes item, BinaryData audioData)
    {
        if (_enqueuedItem.LocalId < item.LocalId)
        {
            ClearPlayback();
            _enqueuedItem.Set(item);
            _audioOutputTask.NotifyItemEnqueued(_enqueuedItem);
        }

        if ((_enqueuedItem.LocalId == item.LocalId) && (_clearedItem.LocalId != item.LocalId))
        {
            byte[]? buffer = (audioData is not null) ? audioData.ToArray() : null;
            if ((buffer is not null) && (buffer.Length > 0))
            {
                try
                {
                    _audioOutputTask.GetDevice().Write(buffer);
                }
                catch (InvalidOperationException)
                {
                    ClearPlayback();
                }
            }
        }
    }

    public void WriteToConsole(ItemAttributes item, string text)
    {
        if (_enqueuedItem.LocalId < item.LocalId)
        {
            ClearPlayback();
            _enqueuedItem.Set(item);
            _audioOutputTask.NotifyItemEnqueued(_enqueuedItem);
        }
    }

    public long CancelStopDisposeAll()
    {
        long finishMs = TaskTool.CancelStopDisposeAll(GetTaskList(), STOP_TASK_TIMEOUT);
#if DEBUG
        if (finishMs >= 0)
        {
            DeviceNotifications.Info($"It took {finishMs} ms to close devices.");
        }
        else
        {
            DeviceNotifications.Error("Failed to close all devices.");
        }
#endif
        return finishMs;
    }
}
