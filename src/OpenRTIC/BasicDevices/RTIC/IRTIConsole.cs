namespace OpenRTIC.BasicDevices.RTIC;

public interface IRTIConsole 
    : IRTIOutput
    , IRTISessionEvents
    , IDeviceNotificationWriter
{
}
