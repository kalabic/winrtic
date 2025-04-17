#define PROGRAM_SYNC_SESSION
//#define PROGRAM_ASYNC_SESSION

using OpenRTIC.Config;
using OpenRTIC.Conversation;


public partial class Program
{
    static private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

#if PROGRAM_SYNC_SESSION
    public static void Main(string[] args)
#elif PROGRAM_ASYNC_SESSION
    public static async Task Main(string[] args)
#else
#error Program must be built with PROGRAM_SYNC_SESSION or PROGRAM_ASYNC_SESSION.
#endif
    {
        // Set UTF-8, handle Ctrl-C, etc.
        InitializeEnvironment();


        // Parse command line arguments, load defaults, etc.
        var options = (args.Length == 0)
            ? ConversationOptions.GetDefaultOptions()
            : GetProgramOptionsWithCommandLineArguments(args);
        if (options is null)
        {
            // This happens when parsing of command line arguments has failed. Just exit because
            // it prints help message on the console in this case.
            return;
        }

        if (!options.NotNull)
        {
            if (!String.IsNullOrEmpty(options?._errorMessage))
            {
                RTIConsole.WriteLine(options?._errorMessage);
            }
            else
            {
                RTIConsole.WriteLine(" * Error: Unknown error occured related to program options.");
            }
            return;
        }


#if PROGRAM_SYNC_SESSION
        RunSession(options, cancellationTokenSource.Token);
#elif PROGRAM_ASYNC_SESSION
        await RunSessionAsync(options, cancellationTokenSource.Token);
#endif
    }

#if PROGRAM_SYNC_SESSION
    /// <summary>
    /// Starts a conversation session with given options.
    /// </summary>
    /// <param name="options">Provided using <see cref="GetDefaultProgramOptions"/> or <see cref="GetProgramOptionsWithCommandLineArguments"/>.</param>
    /// <param name="cancellationToken">Hooked to Ctrl-C handler in <see cref="InitializeEnvironment"/></param>
    private static void RunSession(ConversationOptions options, CancellationToken cancellationToken)
    {
        if (options._client is null || options._session is null)
        {
            return;
        }

        options.PrintApiConfigSourceInfo();

        //
        // Start conversation session. Can you hear me?
        //
        ConversationShell? conversation = ConversationShell.RunSession(RTIConsole, options, cancellationToken);
        if (conversation is null)
        {
            RTIConsole.WriteLine($" * Error: Client failed to connect and start a session.");
            return;
        }

        long finishMs = conversation.FinishSession();
        conversation.Dispose();
        RTIConsole.WriteLine("[DONE (" + finishMs + ")]");
    }

#elif PROGRAM_ASYNC_SESSION
    /// <summary>
    /// Starts a conversation session with given options.
    /// </summary>
    /// <param name="options">Provided using <see cref="GetDefaultProgramOptions"/> or <see cref="GetProgramOptionsWithCommandLineArguments"/>.</param>
    /// <param name="cancellationToken">Hooked to Ctrl-C handler in <see cref="InitializeEnvironment"/></param>
    private static async Task RunSessionAsync(ConversationOptions options, CancellationToken cancellationToken)
    {
        if (options._client is null || options._session is null)
        {
            return;
        }

        options.PrintApiConfigSourceInfo();

        //
        // Start conversation session. Can you hear me?
        //
        ConversationShell? conversation = ConversationShell.RunSessionAsync(RTIConsole, options, cancellationToken);
        if (conversation is null)
        {
            RTIConsole.WriteLine($" * Error: Client failed to connect and start a session.");
            return;
        }

        var awaiter = conversation.GetAwaiter();
        if (awaiter is not null)
        {
            await awaiter;
        }

        conversation.FinishSession();
        conversation.Dispose();
    }
#endif
}
