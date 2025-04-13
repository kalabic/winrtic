using OpenAI.RealtimeConversation;
using OpenRTIC.BasicDevices;
using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002


public class ConversationUpdatesReceiverTask : ConversationUpdatesReceiver
{
    private const int AUDIO_INPUT_STREAM_BUFFER = 4096;

    private TaskWithEvents? _updatesReceiverTask = null;

    private TaskWithEvents? _sendAudioTask = null;

    private TaskWithEvents? _inputAudioTask = null;

    private Stream _audioInputStream;


    public ConversationUpdatesReceiverTask(RealtimeConversationSession session,
                                           Stream audioInputStream,
                                           CancellationToken cancellation)
        : base(session, cancellation)
    {
        this._audioInputStream = audioInputStream;

        //
        // Events sent from this class.
        //
        ReceiverEvents.EnableInvokeFor<SendAudioTaskFinished>();
        ReceiverEvents.EnableInvokeFor<InputAudioTaskFinished>();

#if DEBUG
        SetLabel("Conversation Updates Receiver");
#endif
    }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            _updatesReceiverTask?.Dispose();
            _sendAudioTask?.Dispose();
            _inputAudioTask?.Dispose();
            _audioInputStream.Dispose();
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    public void Run()
    {
        CreateUpdatesReceiverTask();
        _updatesReceiverTask?.RunSynchronously();
    }

    public Task RunAsync()
    {
        CreateUpdatesReceiverTask();
        if (_updatesReceiverTask is not null)
        {
            _updatesReceiverTask.Start();
            return _updatesReceiverTask;
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    public List<TaskWithEvents> GetTaskList()
    {
        List<TaskWithEvents> list = new();
        list.Add(this);
        if (_updatesReceiverTask is not null)
        {
            list.Add(_updatesReceiverTask);
        }
        if (_sendAudioTask is not null)
        {
            list.Add(_sendAudioTask);
        }
        if (_inputAudioTask is not null)
        {
            list.Add(_inputAudioTask);
        }
        return list;
    }

    private void CreateUpdatesReceiverTask()
    {
        if (_updatesReceiverTask is null)
        {
            _updatesReceiverTask = new ActionTask((actionCancellation) => { ReceiveUpdates(actionCancellation); });
#if DEBUG
            _updatesReceiverTask.SetLabel("Updates Receiver");
#endif
        }
        else
        {
            throw new InvalidOperationException("Updates receiver task already created.");
        }
    }

    public void StartAudioInputTask()
    {
        if (_updatesReceiverTask is null)
        {
            throw new InvalidOperationException("Updates receiver task does not exist.");
        }
        else if (_updatesReceiverTask.Status != TaskStatus.Running)
        {
            throw new InvalidOperationException("Updates receiver task is not running.");
        }

        if (_sendAudioTask is not null || _inputAudioTask is not null)
        {
            throw new InvalidOperationException("Audio input task already created.");
        }

        //
        // An intermediate buffer between 'send audio' task and input audio source (microphone). TODO: Will be useful later.
        //
        Stream audioInputBuffer = new AudioStreamBuffer(AudioFormat, AUDIO_INPUT_BUFFER_SECONDS, Cancellation.MicrophoneToken);

        //
        // A task that reads input audio from intermediate buffer and sends it to the server.
        //
        _sendAudioTask = new ActionTask((actionCancellation) =>
        {
            SendAudioInput(audioInputBuffer, Cancellation.MicrophoneToken);
        });
#if DEBUG
        _sendAudioTask.SetLabel("Send Audio");
#endif
        _sendAudioTask.StartAndFinishWithAction( () => 
            { ReceiverEvents.Invoke<SendAudioTaskFinished>(new SendAudioTaskFinished()); });

        //
        // A task that reaches out for input audio data and writes it into internal buffer.
        //
        _inputAudioTask = new ActionTask((actionCancellation) =>
        {
            HandleSessionExceptions(() => 
            {
                byte[] buffer = new byte[AUDIO_INPUT_STREAM_BUFFER];

                while ((this.ReceiverState == ConversationReceiverState.Connected) &&
                        !Cancellation.MicrophoneToken.IsCancellationRequested &&
                        !actionCancellation.IsCancellationRequested)
                {
                    // 'Read' will block until a minimum of data is available.
                    if ((_audioInputStream.Read(buffer, 0, AUDIO_INPUT_STREAM_BUFFER) > 0) && audioInputBuffer.CanWrite)
                    {
                        audioInputBuffer.Write(buffer, 0, AUDIO_INPUT_STREAM_BUFFER);
                    }
                    else
                    {
                        break;
                    }
                }
            });
        });
#if DEBUG
        _inputAudioTask.SetLabel("Input Audio");
#endif
        _inputAudioTask.StartAndFinishWithAction( () =>
            { ReceiverEvents.Invoke<InputAudioTaskFinished>(new InputAudioTaskFinished()); });
    }
}
