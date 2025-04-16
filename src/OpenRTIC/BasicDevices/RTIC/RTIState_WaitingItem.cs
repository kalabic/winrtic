namespace OpenRTIC.BasicDevices.RTIC;

public class RTIState_WaitingItem : RTIStateWithTimer
{
    public bool HasUserTranscript { get { return !String.IsNullOrEmpty(_userTranscript); } }

    private string _userTranscript = "";

    public string GetUserTranscript()
    {
        var result = _userTranscript;
        _userTranscript = "";
        return result;
    }

    protected override void OnTimer(Object? source, System.Timers.ElapsedEventArgs e)
    {
        Console.Write(WaitingConsolePrompt.GetProgressPrompt());
    }

    override public void Enter()
    {
        _timer.Start();
    }

    override public void Exit()
    {
        Console.Write("\r      \r");
        base.Exit();
    }

    override public IRTIState ProcessSessionEvent(RTISessionEvent messageType, IRTIStateCollection stateCollection)
    {
        switch (messageType)
        {
            case RTISessionEvent.ItemStarted:
                return stateCollection.RTIState_WritingItem;

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
        else if (type == RTIOut.User && (message is not null))
        {
            _userTranscript += message;
        }
#if DEBUG
        else
        {
            // TODO: Agent message received before item started!
            throw new NotImplementedException();
        }
#endif
    }

    override public void WriteLine(string? message)
    {
        WriteLine(RTIOut.System, message);
    }
}
