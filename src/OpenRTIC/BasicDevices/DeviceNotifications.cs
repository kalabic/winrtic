namespace OpenRTIC.BasicDevices;

public class DeviceNotifications
{
    static public void ExceptionOccured(Exception ex)
    {
        Console.WriteLine(" >>> Exception occured: " + ex.GetType().ToString() + "; Message: " + ex.Message);
    }

    static public void ObjectDisposed(string label)
    {
        Console.WriteLine(" >>> Disposed: " + label);
    }

    static public void ObjectDisposed(object obj)
    {
        Console.WriteLine(" >>> Disposed: " + obj.GetType().ToString());
    }

    static public void TaskFinished(string label, object obj)
    {
        Console.WriteLine(" >>> Task finished: '" + label + "' " + obj.GetType().ToString());
    }

    static public void Error(string errorMessage)
    {
        Console.WriteLine(" >>> Error: " + errorMessage);
    }
}
