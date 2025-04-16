namespace OpenRTIC.BasicDevices.RTIC;

public class RTIState_Inactive : IRTIState
{
    public void Enter() { }

    public void Exit() { }

    public IRTIState ProcessSessionEvent(RTISessionEvent messageType, IRTIStateCollection stateCollection)
    {
        switch (messageType)
        {
            case RTISessionEvent.ConnectingStarted:
                return stateCollection.RTIState_Connecting;

            case RTISessionEvent.SessionFinished:
                break;

            default:
#if DEBUG
                Console.WriteLine("ConsoleState_Inactive: ignoring message: " + messageType.ToString());
#endif
                break;
        }

        return stateCollection.RTIState_CurrentState;
    }

    public void Write(RTIOut type, string message)
    {
#if DEBUG
        throw new ArgumentException("RTIState_Inactive: Unexpected message type " + type.ToString());
#endif
    }

    public void WriteLine(RTIOut type, string? message)
    {
        if (type == RTIOut.System)
        {
            Console.WriteLine(message);
        }
#if DEBUG
        else
        {
            throw new ArgumentException("RTIState_Inactive: Unexpected message type " + type.ToString());
        }
#endif
    }

    public void WriteLine(string? message)
    {
        WriteLine(RTIOut.System, message);
    }
}
