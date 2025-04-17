namespace OpenRTIC.BasicDevices.RTIC;

/// <summary>
/// Console output adjusted to format of realtime interactive conversation between a user and an AI agent.
/// </summary>
public class RTIConsole 
    : IRTIStateCollection
    , IRTIConsole
{
    public IRTIState RTIState_CurrentState { get { return _currentState; } }

    public IRTIState RTIState_Inactive { get { return _inactive; } }

    public IRTIState RTIState_Connecting { get { return _connecting; } }

    public IRTIState RTIState_WaitingItem { get { return _waitingItem; } }

    public IRTIState RTIState_WritingItem { get { return _writingItem; } }


    private object _locker = new object();

    private IRTIState _currentState;

    private RTIState_Inactive _inactive;

    private RTIState_Connecting _connecting;

    private RTIState_WaitingItem _waitingItem;

    private RTIState_WritingItem _writingItem;

    public RTIConsole()
    {
        _inactive = new RTIState_Inactive();
        _connecting = new RTIState_Connecting();
        _waitingItem = new RTIState_WaitingItem();
        _writingItem = new RTIState_WritingItem();

        _currentState = _inactive;
    }

    private void ProcessSessionEvent(RTISessionEvent sessionEvent, string? message)
    {
        lock (_locker)
        {
            IRTIState nextState = _currentState.ProcessSessionEvent(sessionEvent, this);
            if (nextState != _currentState)
            {
                bool hasUserTranscript = false;
                if (_currentState == _waitingItem)
                {
                    hasUserTranscript = _waitingItem.HasUserTranscript;
                }

                _currentState.Exit();
                _currentState = nextState;
                _currentState.Enter();

                if (sessionEvent == RTISessionEvent.ItemStarted && !String.IsNullOrEmpty(message) && _currentState == _writingItem)
                {
                    _writingItem.WriteItemId(message);
                    message = null;
                }

                if (hasUserTranscript && (_currentState == _writingItem))
                {
                    _writingItem.WriteLine(RTIOut.User, _waitingItem.GetUserTranscript());
                }
            }
            if (message is not null)
            {
                _currentState.WriteLine(message);
            }
        }
    }

    public void WriteNotification(string message)
    {
        WriteLine(RTIOut.System, message);
    }

    //
    // IRTIOutput interface
    //

    public void Write(RTIOut type, string message)
    {
        lock (_locker)
        {
            _currentState.Write(type, message);
        }
    }

    public void WriteLine(RTIOut type, string? message)
    {
        lock (_locker)
        {
            _currentState.WriteLine(type, message);
        }
    }

    public void WriteLine(string? message)
    {
        lock (_locker)
        {
            _currentState.WriteLine(message);
        }
    }

    //
    // IRTISessionOutput interface
    //

    public void ConnectingStarted()
    {
        ProcessSessionEvent(RTISessionEvent.ConnectingStarted, null);
    }

    public void ConnectingFailed(string message)
    {
        ProcessSessionEvent(RTISessionEvent.ConnectingFailed, message);
    }

    public void SessionStarted(string message)
    {
        ProcessSessionEvent(RTISessionEvent.SessionStarted, message);
    }

    public void SessionFinished(string message)
    {
        ProcessSessionEvent(RTISessionEvent.SessionFinished, message);
    }

    public void ItemStarted(string itemId)
    {
#if DEBUG_VERBOSE
        ProcessSessionEvent(RTISessionEvent.ItemStarted, itemId);
#else
        ProcessSessionEvent(RTISessionEvent.ItemStarted, null);
#endif
    }

    public void ItemFinished()
    {
        ProcessSessionEvent(RTISessionEvent.ItemFinished, null);
    }
}
