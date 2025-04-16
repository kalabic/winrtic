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

    private const int STOP_TASK_TIMEOUT = 10000;

    public EventCollection ReceiverQueueEvents { get { return _receiverQueueEvents; } }

    public EventCollection ReceiverEvents { get { return _receiver.ReceiverEvents; } }

    public ForwardedEventQueue Queue {  get { return _receiver; } }

    private const int AUDIO_INPUT_STREAM_BUFFER = 4096;

    private TaskWithEvents? _updatesReceiverTask = null;

    private TaskWithEvents? _sendAudioTask = null;

    private TaskWithEvents? _inputAudioTask = null;

    private int _audioTaskCount = 0;

    private Stream _audioInputStream;

    private RealtimeConversationClient _client;

    private ConversationUpdatesReceiver _receiver;

    private CancellationToken _cancellation;

    private EventCollection _receiverQueueEvents = new();

    public ConversationUpdatesReceiverTask(RealtimeConversationClient client,
                                           Stream audioInputStream,
                                           CancellationToken cancellation)
    {
        this._client = client;
        this._audioInputStream = audioInputStream;
        this._receiver = new ConversationUpdatesReceiver();
        this._cancellation = cancellation;

        //
        // Events sent from this class.
        //
        _receiverQueueEvents.EnableInvokeFor<InputAudioTaskFinished>();
        _receiverQueueEvents.EnableInvokeFor<FailedToConnect>();

        _receiver.ReceiverEvents.Connect(
            _receiver.NewQueuedEventForwarder<InputAudioTaskFinished>( (_, _) => HandleEvent_InputAudioTaskFinished() ));

        _receiver.ReceiverEvents.Connect(
            _receiver.NewQueuedEventForwarder<FailedToConnect>((_, _) => HandleEvent_FailedToConnect() ));
    }

    public void Dispose()
    {
        _updatesReceiverTask?.Dispose();
        _sendAudioTask?.Dispose();
        _inputAudioTask?.Dispose();
        _audioInputStream.Dispose();
        _receiver.Dispose();
    }

    /// <summary>
    /// Runs conversation session synchronously. By the time it returns, complete shutdown should have been initiated and done.
    /// </summary>
    public void Run()
    {
        StartUpdatesReceiverTask();
        _receiver.Run();
    }

    public void RunAsync()
    {
        StartUpdatesReceiverTask();
        _receiver.RunAsync();

        // Assert all tasks here are complete when main task ends.
        var actionAwaiter = _receiver.GetAwaiter();
        if (actionAwaiter is not null)
        {
            actionAwaiter.TaskEvents.Connect<TaskStateUpdate>( (_, update) => AssertAllTasksComplete(update) );
        }
    }

    public Task? GetAwaiter()
    {
        return _receiver.GetAwaiter();
    }

    /// <summary>
    /// Initiates end of conversation session and returns immediatelly. Should be used only when receiver is running
    /// in asynchronous mode. Shutdown always begings with stopping audio input tasks. In fact, if they are completed 
    /// or broken for any reason that alone should trigger end of session and complete shutdown by itself.
    /// </summary>
    public void Cancel()
    {
        _receiver.CancelMicrophone();
        _receiver.FinishReceiver();
    }

    /// <summary>
    /// List of all tasks started by this class, with the exception of the 'awaiter' task, 'awaiter' exists when receiver is
    /// running in asynchronous mode and should not be included in this list.
    /// </summary>
    /// <returns></returns>
    private List<TaskWithEvents> GetTaskList()
    {
        List<TaskWithEvents> list = new();
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

    private void AssertAllTasksComplete(TaskStateUpdate update)
    {
        if (update == TaskStateUpdate.Finished)
        {
            var taskList = GetTaskList();
            foreach (var task in taskList)
            {
                if (!task.IsCompleted)
                {
                    task.Cancel(); // Here just cancel and don't wait. TODO: Notify
#if DEBUG
                    throw new InvalidOperationException();
#endif
                }
            }
        }
    }

    /// <summary>
    /// Throws exception if updates receiver task already exists.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void StartUpdatesReceiverTask()
    {
        if (_updatesReceiverTask is null)
        {
            _updatesReceiverTask = new ActionTask((actionCancellation) => { UpdatesReceiverEntry(actionCancellation); });
#if DEBUG
            _updatesReceiverTask.SetLabel("Updates Receiver");
#endif
            _updatesReceiverTask.Start();
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

        RealtimeConversationSession? session = null;

#if DEBUG_VERBOSE
        long startTimeMs = 0;
        var startStopwatch = new Stopwatch();
        startStopwatch.Start();
#endif

        startWatchdog.Start();
        try
        {
            session = _client.StartConversationSession(startCanceller.Token);
            var options = ConversationSessionConfig.GetDefaultConversationSessionOptions();
            session.ConfigureSession(options, startCanceller.Token);
        }
        catch (TaskCanceledException)
        {
            //
            // Cancellation by watchdog will throw here.
            //
            session?.Dispose();
            _receiver.FailedToConnect("Error: Network timeout. No connection or server not responding.");
            return;
        }
        catch (WebSocketException ex)
        {
            //
            // When OpenAI.RealtimeConversation client gives up, it will throw here.
            //
            session?.Dispose();
            _receiver.FailedToConnect(TaskTool.BuildMultiLineExceptionErrorString(ex));
            return;
        }
        finally
        {
            startWatchdog.Cancel();
#if DEBUG_VERBOSE
            startStopwatch.Stop();
            startTimeMs = startStopwatch.ElapsedMilliseconds;
            DeviceNotifications.Info($" Start Stopwatch: {startTimeMs}");
#endif
        }

        // 'Updates receiver' object has an additional task for invoking specific events.
        _receiver.SetSession(session);

        // To have event handlers be invoked from that additional task, it is necessary to register event forwarders.
        _receiver.ForwardToOtherUsingQueue(_receiverQueueEvents);

        // 'Session Started Update' event is a good time to start sending microphone input to the server.
        _receiverQueueEvents.Connect<ConversationSessionStartedUpdate>((_, _) => StartAudioInputTask());

        _receiver.ReceiveUpdates(receiverTaskCancellation);
    }

    /// <summary>
    /// Invoked from event handler for <see cref="ConversationSessionStartedUpdate"/>. Memeber <see cref="UpdatesReceiverEntry"/>
    /// prepares it if session was created sucessfully.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void StartAudioInputTask()
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

    private void AssureAudioCancelled()
    {
        _receiver.CancelMicrophone();

        if (Interlocked.Decrement(ref _audioTaskCount) == 0)
        {
            _receiver.AudioInputFinished();
        }
    }

    /// <summary>
    /// This ends it all with main message queue included.
    /// <para>NOTE: This is internal event handler, user of public API or any task othen than internal message queue
    /// should never end up in it.</para>
    /// </summary>
    private void HandleEvent_InputAudioTaskFinished()
    {
        _receiver.FinishReceiver(); // This should start graceful shutdown of '_updatesReceiverTask'.

        InternalCancelStopDisposeAll();
        _receiver.CloseMessageQueue(); // The end.
    }

    private void HandleEvent_FailedToConnect()
    {
        InternalCancelStopDisposeAll();
        _receiver.CloseMessageQueue(); // The end.
    }

    private void InternalCancelStopDisposeAll()
    {
        var taskList = GetTaskList();
#if !DEBUG
        TaskTool.CancelStopDisposeAll(taskList, STOP_TASK_TIMEOUT);
#else
        long finishMs = TaskTool.CancelStopDisposeAll(taskList, STOP_TASK_TIMEOUT);
        if (finishMs >= 0)
        {
            DeviceNotifications.Info($"It took {finishMs} ms to close session.");
        }
        else
        {
            DeviceNotifications.Error("Failed to finish session. Device tasks still running.");
        }
#endif
    }
}
