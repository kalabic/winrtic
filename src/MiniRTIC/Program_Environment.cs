using System.Text;

namespace MiniRTIC;

public partial class Program
{
    static private int ctrlcWatchdog = 0;

    static private void InitializeEnvironment()
    {
        // Enable Unicode in Windows console.
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleCancelEventHandler sessionCanceler = (sender, e) =>
        {
            programCanceller.Cancel();

            if (ctrlcWatchdog <= 3)
            {
#if DEBUG
                Console.Write("[ Ctrl-C ]");
#endif
                e.Cancel = true; // Execution continues after the delegate.
                ctrlcWatchdog++;
            }
            else
            {
#if DEBUG
                Console.Write("[ *** Ctrl-C *** ]");
#endif
                e.Cancel = false; // Execution should not continue after the delegate.
            }
        };
        Console.CancelKeyPress += sessionCanceler;
    }
}
