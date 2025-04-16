using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.BasicDevices.RTIC;

public class RTIState_WritingItem : RTIStateWithTimer
{
    private bool _waitingTranscript = false;

    private string _agentBuffer = "";

    protected override void OnTimer(Object? source, System.Timers.ElapsedEventArgs e)
    {
        Console.Write(WaitingConsolePrompt.GetProgressPrompt());
    }

    public void WriteItemId(string itemId)
    {
        // Console.WriteLine("Item Id: " + itemId);
    }

    override public void Enter()
    {
        string itemHeader = "[---- " + DateTime.Now.ToLongTimeString() + " ---- " + DateTime.Now.ToShortDateString() + " ----]\n";
        // Align text right.
        Console.CursorLeft = Console.BufferWidth - itemHeader.Length - 5;
        Console.WriteLine(itemHeader);
        _timer.Start();
        _waitingTranscript = true;
    }

    override public void Exit()
    {
        if (_waitingTranscript)
        {
            _timer.Stop();
            Console.Write("\r      \r");
            _waitingTranscript = false;
            // Console.WriteLine("  USER: ");
            // Console.WriteLine("");
            Console.WriteLine(" AGENT: " + _agentBuffer);
            Console.WriteLine("");
            _agentBuffer = "";
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine();
        }
        base.Exit();
    }

    override public IRTIState ProcessSessionEvent(RTISessionEvent messageType, IRTIStateCollection stateCollection)
    {
        switch (messageType)
        {
            case RTISessionEvent.ItemFinished:
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
        if (type == RTIOut.Agent)
        {
            if (_waitingTranscript)
            {
                _agentBuffer += message;
            }
            else
            {
                Console.Write(message);
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    override public void WriteLine(RTIOut type, string? message)
    {
        if (type == RTIOut.User)
        {
            if (_waitingTranscript)
            {
                Console.Write("\r      \r");
                _timer.Stop();
                _waitingTranscript = false;
                Console.WriteLine("  USER: " + ((message is not null) ? message : ""));
                Console.Write(" AGENT: " + _agentBuffer);
                _agentBuffer = "";
            }
            else
            {
                Console.WriteLine("\n[UNEXPECTED TRANSCRIPT UPDATE]\n");
                Console.WriteLine("  USER: " + ((message is not null) ? message : ""));
                Console.Write(" AGENT: ");
            }
        }
        else if (type == RTIOut.System)
        {
            if (_waitingTranscript)
            {
                Console.Write("\r      \r");
            }
            else
            {
                Console.WriteLine();
            }

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
