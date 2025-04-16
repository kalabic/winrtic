namespace OpenRTIC.BasicDevices.RTIC;

/// <summary>
/// A collection of all states for <see cref="RTIConsole"/>.
/// </summary>
public interface IRTIStateCollection
{
    public IRTIState RTIState_CurrentState { get; }

    public IRTIState RTIState_Inactive { get; }

    public IRTIState RTIState_Connecting { get; }

    public IRTIState RTIState_WaitingItem { get; }

    public IRTIState RTIState_WritingItem { get; }
}
