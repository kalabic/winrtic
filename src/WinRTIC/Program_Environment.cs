using OpenRTIC.BasicDevices;
using OpenRTIC.BasicDevices.RTIC;
using System.Runtime.InteropServices;
using System.Text;

public partial class Program
{
    static private RTIConsole RTIConsole = new();

    //
    // https://www.meziantou.net/detecting-console-closing-in-dotnet.htm
    //
    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

    private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

    private enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    static private void InitializeEnvironment()
    {
        // Enable Unicode in Windows console.
        Console.OutputEncoding = Encoding.UTF8;

        // TODO: SetConsoleCtrlHandler(Handler, true);

        ConsoleCancelEventHandler sessionCanceler = (sender, e) =>
        {
            e.Cancel = true; // Execution continues after the delegate.
            cancellationTokenSource.Cancel();
#if DEBUG
            Console.Write("[ *** Ctrl-C *** ]");
#endif
        };
        Console.CancelKeyPress += sessionCanceler;

        DeviceNotifications.writer = RTIConsole;
    }

#if false // TODO: Priority shutdown of everything.
    private static bool Handler(CtrlType signal)
    {
        // https://learn.microsoft.com/en-us/windows/console/handlerroutine

        int timeoutMs = -1;
        switch (signal)
        {
            case CtrlType.CTRL_BREAK_EVENT:
                timeoutMs = -1;
                Console.Write("[ *** BREAK *** ]");
                break;
            case CtrlType.CTRL_LOGOFF_EVENT:
                timeoutMs = 500;
                Console.Write("[ *** LOGOFF *** ]");
                break;
            case CtrlType.CTRL_SHUTDOWN_EVENT:
                timeoutMs = 20000;
                Console.Write("[ *** SHUTDOWN *** ]");
                break;
            case CtrlType.CTRL_CLOSE_EVENT:
                timeoutMs = 5000;
                Console.Write("[ *** CLOSE *** ]");
                break;
            default:
                return false;
        }

        if (conversation is not null)
        {
            conversation.CancelCurrentSession();
            conversation.FinishSession(timeoutMs);
        }

        return true;
    }
#endif
}
