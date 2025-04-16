using OpenRTIC.BasicDevices;
using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation.Devices;

public abstract class DeviceTask<TDevice> : TaskWithEvents
{
    protected TDevice Device;

    public DeviceTask(TDevice device)
    {
        this.Device = device;
    }

    protected override void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing)
        {
            if ((Device is not null) && (Device is IDisposable disposableDevice))
            {
                disposableDevice.Dispose();
            }
#if DEBUG_VERBOSE
            DeviceNotifications.ObjectDisposed(this);
#endif
        }

        // Release unmanaged resources.
        base.Dispose(disposing);
    }

    public TDevice GetDevice()
    {
        return Device;
    }
}
