#define PROGRAM_SYNC_SESSION
//#define PROGRAM_ASYNC_SESSION

using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using OpenRTIC.Config;
using OpenRTIC.Conversation;

#pragma warning disable OPENAI002


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
        ProgramOptions options = (args.Length == 0)
            ? GetDefaultProgramOptions()
            : GetProgramOptionsWithCommandLineArguments(args);


        if (!options.NotNull)
        {
            if (!String.IsNullOrEmpty(options?.errorMessage))
            {
                RTIConsole.WriteLine(options?.errorMessage);
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
    private static void RunSession(ProgramOptions options, CancellationToken cancellationToken)
    {
        if (options.client is null || options.session is null)
        {
            return;
        }

        options.PrintApiConfigSourceInfo();

        RealtimeConversationClient client = GetConfiguredClient(options.client);

        //
        // Start conversation session. Can you hear me?
        //
        ConversationShell? conversation = ConversationShell.RunSession(RTIConsole, client, options.session, cancellationToken);
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
    private static async Task RunSessionAsync(ProgramOptions options, CancellationToken cancellationToken)
    {
        if (options.client is null || options.session is null)
        {
            return;
        }

#if DEBUG
        options.PrintApiConfigSourceInfo();
#endif

        RealtimeConversationClient client = GetConfiguredClient(options.client);

        //
        // Start conversation session. Can you hear me?
        //
        ConversationShell? conversation = ConversationShell.RunSessionAsync(RTIConsole, client, options.session, cancellationToken);
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

    private static RealtimeConversationClient GetConfiguredClient(ClientApiConfig options)
    {
        switch(options.Type)
        {
            case EndpointType.AzureOpenAIWithEntra:
                return GetConfiguredClientForAzureOpenAIWithEntra(options.AOAIEndpoint, options.AOAIDeployment);

            case EndpointType.AzureOpenAIWithKey:
                return GetConfiguredClientForAzureOpenAIWithKey(options.AOAIEndpoint, options.AOAIDeployment, options.AOAIApiKey);

            case EndpointType.OpenAIWithKey:
                return GetConfiguredClientForOpenAIWithKey(options.OAIApiKey);
        }

        throw new InvalidOperationException(
                    $"Incomplete or missing '" + DEFAULT_CONVERSATIONAPI_FILENAME + "' or environment configuration.Please provide one of:\n"
                    + " - AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY\n"
                    + " - OPENAI_API_KEY");
    }
    private static RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithEntra(
        string aoaiEndpoint,
        string? aoaiDeployment)
    {
        RTIConsole.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        RTIConsole.WriteLine($" * Using Entra token-based authentication (AZURE_OPENAI_USE_ENTRA)");
        RTIConsole.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new DefaultAzureCredential());
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithKey(
        string aoaiEndpoint,
        string? aoaiDeployment,
        string aoaiApiKey)
    {
        RTIConsole.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        RTIConsole.WriteLine($" * Using API key (AZURE_OPENAI_API_KEY): {aoaiApiKey[..5]}**");
        RTIConsole.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForOpenAIWithKey(string oaiApiKey)
    {
        string oaiEndpoint = "https://api.openai.com/v1";
        RTIConsole.WriteLine($" * Connecting to OpenAI endpoint (OPENAI_ENDPOINT): {oaiEndpoint}");
        RTIConsole.WriteLine($" * Using API key (OPENAI_API_KEY): {oaiApiKey[..5]}**");

        OpenAIClient aoaiClient = new(new ApiKeyCredential(oaiApiKey));
        return aoaiClient.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
    }
}
