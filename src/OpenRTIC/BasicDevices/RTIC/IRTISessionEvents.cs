namespace OpenRTIC.BasicDevices.RTIC;

/// <summary>
/// This should be used by main program to notify <see cref="RTIConsole"/> about general 
/// state of conversation session.
/// </summary>
public interface IRTISessionEvents
{
    public void ConnectingStarted();

    public void ConnectingFailed(string message);

    public void SessionStarted(string message);

    public void SessionFinished(string message);

    public void ItemStarted(string itemId);

    public void ItemFinished();
}
