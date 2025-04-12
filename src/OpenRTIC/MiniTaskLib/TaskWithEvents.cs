using OpenRTIC.BasicDevices;

namespace OpenRTIC.MiniTaskLib;

public enum TaskStateUpdate
{
    Started,
    Canceled,
    Finished
}

public abstract class TaskWithEvents : TaskBase
{
    public EventCollection TaskEvents { get { return _taskEvents; } }

    public string TaskLabel { get { return _label; } }


    private EventCollection _taskEvents = new();

    private CancellationTokenSource _cancellationTokenSource;

    private string _label = "";


    public TaskWithEvents(CancellationToken? cancellation = null)
    // 'false' so in base class both CanBeCanceled and IsCancellationRequested will be false.
        : base(new CancellationToken(false))
    {
        if (cancellation is not null)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource((CancellationToken)cancellation);
        }
        else
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        _taskEvents.EnableInvokeFor<TaskExceptionOccured>();
        _taskEvents.EnableInvokeFor<TaskStateUpdate>();
    }

    protected CancellationToken GetPrivateCancellationToken()
    {
        return _cancellationTokenSource.Token;
    }

    public void SetLabel(string label)
    {
        _label = label;
    }

    override protected void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            _taskEvents.Clear();
#if DEBUG_VERBOSE
            DeviceNotifications.ObjectDisposed(this);
#endif
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    public void StartAndFinishWithAction(Action finishAction)
    {
        _taskEvents.ConnectEventHandler<TaskStateUpdate>((_, update) =>
        {
            if (update == TaskStateUpdate.Finished)
            {
                finishAction();
            }
        });

        base.Start();
    }

    public TaskWithEvents StopDeviceTask(CancellationToken stopActionCancellation)
    {
        ActionTask stopTask = new ActionTask((actionCancellation) =>
        {
            try
            {
                Cancel();
                if (!IsCompleted)
                {
                    Wait(actionCancellation);
                }
            }
            catch (OperationCanceledException ex)
            {
                DeviceNotifications.ExceptionOccured(ex);
            }
            catch (AggregateException ex)
            {
                DeviceNotifications.ExceptionOccured(ex);
            }
            finally
            {
                Dispose();
            }
        }, stopActionCancellation);
#if DEBUG
        stopTask.SetLabel("Stop Task For [" + _label + "]");
#endif
        stopTask.Start();
        return stopTask;
    }

    //
    // Start() -> StopDeviceTask() {
    //     Cancel() -> Wait() -> Dispose()
    // }
    //

    private void Cancel()
    {
        if (!IsCancellationRequested())
        {
            _cancellationTokenSource.Cancel();
            _taskEvents.Invoke(TaskStateUpdate.Canceled);
        }
    }

    protected bool IsCancellationRequested()
    {
        return _cancellationTokenSource.IsCancellationRequested;
    }

    //
    // Entry for main task function
    //
    override protected void TaskFunction()
    {
        _taskEvents.Invoke(TaskStateUpdate.Started);
        try
        {
            TaskFunction(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            NotifyExceptionOccurred(ex);
        }
        _taskEvents.Invoke(TaskStateUpdate.Finished);
#if DEBUG_VERBOSE
        DeviceNotifications.TaskFinished(_label, this);
#endif
    }

    protected abstract void TaskFunction(CancellationToken cancellation);

    virtual protected void NotifyExceptionOccurred(Exception ex)
    {
        _taskEvents.Invoke(new TaskExceptionOccured(ex));
    }

    virtual protected void NotifyEventOccurred(EventArgs ev)
    {
        Console.WriteLine(" * Event occurred" + ev.ToString());
    }
}
