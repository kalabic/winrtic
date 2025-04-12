namespace OpenRTIC.MiniTaskLib;

public class ActionTask : TaskWithEvents
{
    private Action<CancellationToken> _action;

    public ActionTask(Action<CancellationToken> action)
    {
        this._action = action;
    }

    public ActionTask(Action<CancellationToken> action, CancellationToken cancellation)
        : base(cancellation)
    {
        this._action = action;
    }

    protected override void TaskFunction(CancellationToken cancellation)
    {
        _action(cancellation);
    }
}
