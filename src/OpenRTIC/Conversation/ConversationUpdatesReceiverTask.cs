using OpenAI.RealtimeConversation;
using OpenRTIC.BasicDevices;
using OpenRTIC.MiniTaskLib;
using OpenRTIC.Config;
using System.Net.WebSockets;
using System.Diagnostics;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002


public class ConversationUpdatesReceiverTask : IDisposable
{
    // Just in case, let's keep connection opening timeout under 20  sec.
    private const int START_TASK_TIMEOUT = 20000;

    public EventCollection ReceiverEvents { get { return _receiverEvents; } }

    private const int AUDIO_INPUT_STREAM_BUFFER = 4096;

    private TaskWithEvents? _updatesReceiverTask = null;

    private TaskWithEvents? _sendAudioTask = null;

    private TaskWithEvents? _inputAudioTask = null;

    private int _audioTaskCount = 0;

    private Stream _audioInputStream;

    private RealtimeConversationClient _client;

    private ConversationUpdatesReceiver? _receiver = null;

    private CancellationToken _cancellation;

    private EventCollection _receiverEvents = new();

    public ConversationUpdatesReceiverTask(RealtimeConversationClient client,
                                           Stream audioInputStream,
                                           CancellationToken cancellation)
    {
        this._client = client;
        this._audioInputStream = audioInputStream;
        this._cancellation = cancellation;

        //
        // Events sent from this class.
        //
        _receiverEvents.EnableInvokeFor<SendAudioTaskFinished>();
        _receiverEvents.EnableInvokeFor<InputAudioTaskFinished>();
        _receiverEvents.EnableInvokeFor<FailedToConnect>();
    }

    public void Dispose()
    {
        _updatesReceiverTask?.Dispose();
        _sendAudioTask?.Dispose();
        _inputAudioTask?.Dispose();
        _audioInputStream.Dispose();
        _receiver?.Dispose();
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

    public void FinishReceiver()
    {
        _receiver?.FinishReceiver();
    }

    public List<TaskWithEvents> GetTaskList()
    {
        List<TaskWithEvents> list = new();
        if (_receiver is not null)
        {
            list.Add(_receiver);
        }
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
            _updatesReceiverTask = new ActionTask((actionCancellation) => { UpdatesReceiverEntry(actionCancellation); });
#if DEBUG
            _updatesReceiverTask.SetLabel("Updates Receiver");
#endif
        }
        else
        {
            throw new InvalidOperationException("Updates receiver task already created.");
        }
    }

    private void UpdatesReceiverEntry(CancellationToken receiverTaskCancellation)
    {
        var startCanceller = new CancellationTokenSource();
        var startWatchdog = new ActionTask((watchdogCancellation) =>
        {
            WaitHandle[] waitHandles = { watchdogCancellation.WaitHandle, 
                                         receiverTaskCancellation.WaitHandle, 
                                         _cancellation.WaitHandle };
            int index = WaitHandle.WaitAny(waitHandles, START_TASK_TIMEOUT);
            if (index != 0) // Only cancellation that is ok is the 'watchdogCancellation'.
            {
                startCanceller.Cancel();
            }
        });
#if DEBUG
        startWatchdog.SetLabel("Start Watchdog");
#endif
        startWatchdog.Start();

        RealtimeConversationSession? session = null;
#if DEBUG_VERBOSE
        long startTimeMs = 0;
        var startStopwatch = new Stopwatch();
        startStopwatch.Start();
#endif
        try
        {
            session = _client.StartConversationSession(startCanceller.Token);
            var options = ConversationSessionConfig.GetDefaultConversationSessionOptions();
            session.ConfigureSession(options, startCanceller.Token);
#if DEBUG_VERBOSE
            startStopwatch.Stop();
            startTimeMs = startStopwatch.ElapsedMilliseconds;
#endif
        }
        catch (TaskCanceledException)
        {
#if DEBUG_VERBOSE
            startStopwatch.Stop();
            startTimeMs = startStopwatch.ElapsedMilliseconds;
#endif
            session?.Dispose();
            return;
        }
        catch (WebSocketException ex)
        {
#if DEBUG_VERBOSE
            startStopwatch.Stop();
            startTimeMs = startStopwatch.ElapsedMilliseconds;
#endif
            session?.Dispose();
            ReceiverEvents.Invoke<FailedToConnect>(new FailedToConnect(ex.Message));
            return;
        }
        finally
        {
            startWatchdog.Cancel();
#if DEBUG_VERBOSE
            Console.WriteLine($" >>> Start Stopwatch: {startTimeMs}");
#endif
        }

        // 'Updates receiver' object has an additional task for invoking specific events.
        _receiver = new ConversationUpdatesReceiver(session);

        // To have event handlers be invoked from that additional task, it is necessary to register event forwarders.
        _receiver.ForwardToOtherUsingQueue(_receiverEvents);

        // 'Session Started Update' event is a good time to start sending microphone input to the server.
        _receiverEvents.ConnectEventHandler<ConversationSessionStartedUpdate>((_, _) => StartAudioInputTask());

        _receiver.ReceiveUpdates(receiverTaskCancellation);
    }

    public void StartAudioInputTask()
    {
        if (_receiver is null)
        {
            throw new InvalidOperationException("Updates receiver object does not exist.");
        }
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
        Stream audioInputBuffer = new AudioStreamBuffer(
            ConversationSessionConfig.AudioFormat, ConversationSessionConfig.AUDIO_INPUT_BUFFER_SECONDS, _receiver.Cancellation.MicrophoneToken);

        //
        // A task that reads input audio from intermediate buffer and sends it to the server.
        //
        _sendAudioTask = new ActionTask((_) =>
        {
            Interlocked.Increment(ref _audioTaskCount);
            _receiver.SendAudioInput(audioInputBuffer, _receiver.Cancellation.MicrophoneToken);
        });
#if DEBUG
        _sendAudioTask.SetLabel("Send Audio");
#endif
        _sendAudioTask.StartAndFinishWithAction( () => 
        {
            AssureAudioCancelled();
        });

        //
        // A task that reaches out for input audio data and writes it into internal buffer.
        //
        _inputAudioTask = new ActionTask((actionCancellation) =>
        {
            Interlocked.Increment(ref _audioTaskCount);
            _receiver.HandleSessionExceptions(() => 
            {
                byte[] buffer = new byte[AUDIO_INPUT_STREAM_BUFFER];

                while ((_receiver.ReceiverState == ConversationReceiverState.Connected) &&
                        !_receiver.Cancellation.MicrophoneToken.IsCancellationRequested &&
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
        {
            AssureAudioCancelled();
        });
    }

    public void AssureAudioCancelled()
    {
        if (Interlocked.Decrement(ref _audioTaskCount) == 0)
        {
            ReceiverEvents.Invoke<InputAudioTaskFinished>(new InputAudioTaskFinished());
        }
        if ((_receiver is not null) && !_receiver.Cancellation.IsMicrophoneCancelled)
        {
            _receiver.Cancellation.CancelMicrophone();
        }
        _receiver?.FinishReceiver();
    }
}
