#define RUN_SYNC

using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using OpenRTIC.Config;
using OpenRTIC.Conversation;
using OpenRTIC.BasicDevices;
using OpenRTIC.BasicDevices.RTIC;

namespace MiniRTIC;

#pragma warning disable OPENAI002

/// <summary>
/// A Minimum viable RealTime Interactive Console for connecting to OpenAI's realtime API.
/// <para>Please provide one of following in your environment variables:</para>
/// <list type = "bullet">
///   <item>AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY</item>
///   <item>OPENAI_API_KEY</item>
/// </list>
/// </summary>
public partial class Program
{
    /// <summary>
    /// Connected to <see cref="Console.CancelKeyPress"/> when <see cref="InitializeEnvironment"/> is invoked.
    /// </summary>
    static private readonly CancellationTokenSource programCanceller = new CancellationTokenSource();

    static private CancellationToken GetCancellationToken() => programCanceller.Token;

#if RUN_SYNC
    public static void Main(string[] args)
#else
    public static async Task Main(string[] args)
#endif
    {
        // Set UTF-8, handle Ctrl-C, etc.
        InitializeEnvironment();

        // OpenAI.RealtimeConversation client with basically default options.
        var config = ClientApiConfig.FromEnvironment();
        var client = GetConfiguredClient(config);

        //
        // Create devices, register to be notified about some receiverQueueEvents and run!
        //

        var speaker = new SpeakerAudioStream(ConversationSessionConfig.AudioFormat, GetCancellationToken());
        var microphone = new MicrophoneAudioStream(ConversationSessionConfig.AudioFormat, GetCancellationToken());
        var updatesReceiver = new ConversationUpdatesReceiverTask(client, microphone, GetCancellationToken());

        //
        // A collection of receiverQueueEvents to listen on, will be invoked from a task that is not used
        // for fetching conversation updates.
        //
        var receiverEvents = updatesReceiver.ReceiverEvents;
        var receiverQueueEvents = updatesReceiver.ReceiverQueueEvents;

        //
        // FailedToConnect
        //
        receiverEvents.ConnectEventHandler<FailedToConnect>((_, update) =>
        {
            RTIConsole.ConnectingFailed(update._message);
        });

        //
        // ConversationSessionStartedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationSessionStartedUpdate>(false, (_, update) =>
        {
            // Notify console output that session has started.
            RTIConsole.SessionStarted(" *\n * Session started (Ctrl-C to finish)\n *");

            // 'Hello there' sample is enqueued into audio input stream when session starts.
            // It is a free sample from https://pixabay.com/sound-effects/quothello-therequot-158832/
            byte[] helloBuffer = MiniRTIC.Properties.Resources.hello_there;
            microphone.ClearBuffer();
            microphone.Write(helloBuffer, 0, helloBuffer.Length);
        });

        //
        // SendAudioTaskFinished
        //
        receiverQueueEvents.ConnectEventHandler<SendAudioTaskFinished>(false, (_, update) =>
        {
            RTIConsole.SessionFinished("Audio input stream is stopped\nSESSION FINISHED");
        });

        //
        // ConversationInputSpeechStartedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationInputSpeechStartedUpdate>(false, (_, update) =>
        {
            // Ratio speaker volume while user is speaking.
            speaker.Volume = 0.3f;
        });

        //
        // ConversationInputSpeechFinishedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationInputSpeechFinishedUpdate>(false, (_, update) =>
        {
            speaker.Volume = 1.0f;
        });

        //
        // ConversationResponseStartedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationResponseStartedUpdate>(false, (_, update) =>
        {
            speaker.ClearBuffer();
            RTIConsole.ItemStarted(update.EventId);
        });

        //
        // ConversationResponseFinishedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationResponseFinishedUpdate>(false, (_, update) =>
        {
            RTIConsole.ItemFinished();
        });

        //
        // ConversationInputTranscriptionFinishedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationInputTranscriptionFinishedUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.Transcript))
            {
                RTIConsole.WriteLine(RTIOut.User, update.Transcript);
            }
        });

        //
        // ConversationInputTranscriptionFailedUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationInputTranscriptionFailedUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.ErrorMessage))
            {
                RTIConsole.WriteLine(RTIOut.User, update.ErrorMessage);
            }
        });

        //
        // ConversationItemStreamingPartDeltaUpdate
        //
        receiverQueueEvents.ConnectEventHandler<ConversationItemStreamingPartDeltaUpdate>(false, (_, update) =>
        {
            if (update.AudioBytes is not null)
            {
                speaker.Write(update.AudioBytes);
            }
            if (!String.IsNullOrEmpty(update.AudioTranscript))
            {
                RTIConsole.Write(RTIOut.Agent, update.AudioTranscript);
            }
            if (!String.IsNullOrEmpty(update.Text))
            {
                throw new NotImplementedException();
            }
        });

#if RUN_SYNC
        RTIConsole.ConnectingStarted();
        updatesReceiver.Run();
#else
        updatesReceiver.RunAsync();

        var awaiter = updatesReceiver.GetAwaiter();
        if (awaiter is not null)
        {
            await awaiter;
        }
#endif

        // 'Close' is invoked from 'Dispose' for Stream based classes
        microphone.Dispose();
        speaker.Dispose();
    }

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
                    $"Incomplete or missing environment configuration.Please provide one of:\n"
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
