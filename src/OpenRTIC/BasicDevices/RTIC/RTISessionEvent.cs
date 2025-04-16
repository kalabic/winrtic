namespace OpenRTIC.BasicDevices.RTIC;

/// <summary>
/// Important events about general state of conversation session.
/// </summary>
public enum RTISessionEvent
{
    ConnectingStarted,
    ConnectingFailed,
    SessionStarted,
    SessionFinished,
    ItemStarted,
    ItemFinished,
}
