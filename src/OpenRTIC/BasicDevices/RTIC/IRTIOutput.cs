namespace OpenRTIC.BasicDevices.RTIC;

/// <summary>
/// Output interface implemented by every state of <see cref="RTIConsole"/>.
/// </summary>
public interface IRTIOutput
{
    public void Write(RTIOut type, string message);

    public void WriteLine(RTIOut type, string message);

    public void WriteLine(string message);
}
