namespace OpenRTIC.MiniTaskLib;

public class TaskExceptionOccured
{
    public readonly Exception Exception;

    public TaskExceptionOccured(Exception ex)
    {
        Exception = ex;
    }
}

public class CloseMessageQueue
{
    public CloseMessageQueue() { }
}
