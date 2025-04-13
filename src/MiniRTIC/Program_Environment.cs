using System.Runtime.InteropServices;
using System.Text;

namespace MiniRTIC;

public partial class Program
{
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

        SetConsoleCtrlHandler(Handler, true);

        ConsoleCancelEventHandler sessionCanceler = (sender, e) =>
        {
#if DEBUG
            Console.Write("[ Ctrl-C ]");
#endif
            e.Cancel = true; // Execution continues after the delegate.
            programCanceller.Cancel();
        };
        Console.CancelKeyPress += sessionCanceler;
    }

    private static bool Handler(CtrlType signal)
    {
        // https://learn.microsoft.com/en-us/windows/console/handlerroutine

        switch (signal)
        {
            case CtrlType.CTRL_BREAK_EVENT:
                Console.Write("[ *** BREAK *** ]");
                break;
            case CtrlType.CTRL_LOGOFF_EVENT:
                Console.Write("[ *** LOGOFF *** ]");
                break;
            case CtrlType.CTRL_SHUTDOWN_EVENT:
                Console.Write("[ *** SHUTDOWN *** ]");
                break;
            case CtrlType.CTRL_CLOSE_EVENT:
                Console.Write("[ *** CLOSE *** ]");
                break;
            default:
                return false;
        }

        return true;
    }
}
