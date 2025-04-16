namespace OpenRTIC.BasicDevices;

public class DeviceNotifications
{
    static public IDeviceNotificationWriter? writer = null;

    static public void ExceptionOccured(Exception ex)
    {
        writer?.WriteNotification(" >>> Exception occured: " + ex.GetType().ToString() + "; Message: " + ex.Message);
    }

    static public void ObjectDisposed(string label)
    {
        writer?.WriteNotification(" >>> Disposed: " + label);
    }

    static public void ObjectDisposed(object obj)
    {
        writer?.WriteNotification(" >>> Disposed: " + obj.GetType().ToString());
    }

    static public void TaskFinished(string label, object obj)
    {
        writer?.WriteNotification(" >>> Task finished: '" + label + "' " + obj.GetType().ToString());
    }

    static public void Error(string errorMessage)
    {
        writer?.WriteNotification(" >>> Error: " + errorMessage);
    }

    static public void Info(string infoMessage)
    {
        writer?.WriteNotification(" >>> Info: " + infoMessage);
    }
}
