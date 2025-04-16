namespace OpenRTIC.BasicDevices.RTIC;

public class RTIState_Connecting : RTIStateWithTimer
{
    private int _onTimerCount = 0;

    override protected void OnTimer(Object? source, System.Timers.ElapsedEventArgs e)
    {
        if (_onTimerCount == 4)
        {
            Console.Write("\r     \r");
            _onTimerCount = 0;
        }
        else
        {
            Console.Write(".");
            _onTimerCount++;
        }
    }

    override public void Enter() 
    { 
        _timer.Start();
    }

    override public void Exit() 
    {
        Console.Write("\r     \r");
        base.Exit();
    }

    override public IRTIState ProcessSessionEvent(RTISessionEvent messageType, IRTIStateCollection stateCollection)
    {
        switch (messageType)
        {
            case RTISessionEvent.ConnectingFailed:
                return stateCollection.RTIState_Inactive;

            case RTISessionEvent.SessionStarted:
                return stateCollection.RTIState_WaitingItem;

            default:
#if DEBUG
                Console.WriteLine("ConsoleState_Connecting: ignoring message: " + messageType.ToString());
#endif
                break;
        }

        return stateCollection.RTIState_CurrentState;
    }

    override public void Write(RTIOut type, string message)
    {
        throw new NotImplementedException();
    }

    override public void WriteLine(RTIOut type, string? message)
    {
        if (type == RTIOut.System)
        {
            Console.Write("\r     \r");
            Console.WriteLine(message);
        }
#if DEBUG
        else
        {
            throw new NotImplementedException();
        }
#endif
    }

    override public void WriteLine(string? message)
    {
        WriteLine(RTIOut.System, message);
    }
}
