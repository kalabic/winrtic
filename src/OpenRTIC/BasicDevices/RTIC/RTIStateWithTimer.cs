using Timer = System.Timers.Timer;

namespace OpenRTIC.BasicDevices.RTIC;

public abstract class RTIStateWithTimer : IRTIState
{
    protected Timer _timer;

    public RTIStateWithTimer()
    {
        _timer = new();
        _timer.Interval = 500;
        _timer.Elapsed += OnTimer;
        _timer.AutoReset = true;
    }

    protected abstract void OnTimer(Object? source, System.Timers.ElapsedEventArgs e);

    //
    // IRTIState interface
    //

    public abstract void Enter();

    public virtual void Exit()
    {
        _timer.Stop();
    }

    public abstract IRTIState ProcessSessionEvent(RTISessionEvent eventType, IRTIStateCollection stateCollection);

    //
    // IRTIOutput interface
    //

    public abstract void Write(RTIOut type, string message);

    public abstract void WriteLine(RTIOut type, string? message);

    public abstract void WriteLine(string? message);
}
