namespace OpenRTIC.BasicDevices.RTIC;

public interface IRTIState : IRTIOutput
{
    public void Enter();

    public void Exit();

    public IRTIState ProcessSessionEvent(RTISessionEvent eventType, IRTIStateCollection stateCollection);
}
