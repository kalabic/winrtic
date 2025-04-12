using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using OpenRTIC.Config;
using OpenRTIC.Conversation;
using OpenRTIC.BasicDevices;
using System.Net.WebSockets;

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
    static private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public static void Main(string[] args)
    {
        // Set UTF-8, handle Ctrl-C, etc.
        InitializeEnvironment();

        var config = ClientApiConfig.FromEnvironment();
        var client = GetConfiguredClient(config);

        // A small console handling text output in a more friendly way.
        var console = new MiniConsole();


        RealtimeConversationSession? session = null;
        try
        {
            var startCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
            session = client.StartConversationSession(startCancellationSource.Token);
            var options = GetDefaultConversationSessionOptions();
            session.ConfigureSession(options, startCancellationSource.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Canceled");
            return;
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine("Connection failed: " + ex.Message);
            return;
        }

        var cancellation = cancellationTokenSource.Token;
        var speaker = new SpeakerAudioStream(ConversationUpdatesReceiver.AudioFormat, cancellation);
        var microphone = new MicrophoneAudioStream(ConversationUpdatesReceiver.AudioFormat, cancellation);
        var updatesReceiver = new ConversationUpdatesReceiverTask(session, microphone, cancellation);

        //
        // 'Hello' sample that will be enqueued into audio input stream when session starts.
        // It is a free sample from https://pixabay.com/sound-effects/quothello-therequot-158832/
        //
        byte[] helloBuffer = MiniRTIC.Properties.Resources.hello_there;

        //
        // A collection of events to listen on, invoked from a task that is not used
        // for fetching conversation updates.
        //
        var events = updatesReceiver.ReceiverEvents;

        //
        // ConversationSessionStartedUpdate
        //
        events.ConnectEventHandler<ConversationSessionStartedUpdate>((_, update) =>
        {
            console.StartSession();
            console.SetStateWaitingItem();

            // Now is a good time to start sending microphone input to the server.
            updatesReceiver.StartAudioInputTask();

            // Start by queing 'Hello there' sample.
            microphone.ClearBuffer();
            microphone.Write(helloBuffer, 0, helloBuffer.Length);
        });

        //
        // SendAudioTaskFinished
        //
        events.ConnectEventHandler<SendAudioTaskFinished>((_, update) =>
        {
            // Receiver will finish after audio input stream is stopped.
            updatesReceiver.FinishReceiver();
        });

        //
        // ConversationInputSpeechStartedUpdate
        //
        events.ConnectEventHandler<ConversationInputSpeechStartedUpdate>((_, update) =>
        {
            // Ratio speaker volume while user is speaking.
            speaker.Volume = 0.3f;
        });

        //
        // ConversationInputSpeechFinishedUpdate
        //
        events.ConnectEventHandler<ConversationInputSpeechFinishedUpdate>((_, update) =>
        {
            speaker.Volume = 1.0f;
        });

        //
        // ConversationResponseStartedUpdate
        //
        events.ConnectEventHandler<ConversationResponseStartedUpdate>((_, update) =>
        {
            speaker.ClearBuffer();
        });

        //
        // ConversationResponseFinishedUpdate
        //
        events.ConnectEventHandler<ConversationResponseFinishedUpdate>((_, update) =>
        {
            console.SetStateWaitingItem();
        });

        //
        // ConversationInputTranscriptionFinishedUpdate
        //
        events.ConnectEventHandler<ConversationInputTranscriptionFinishedUpdate>((_, update) =>
        {
            console.WriteTranscript(update.Transcript);
        });

        //
        // ConversationItemStreamingPartDeltaUpdate
        //
        events.ConnectEventHandler<ConversationItemStreamingPartDeltaUpdate>((_, update) =>
        {
            if (update.AudioBytes is not null)
            {
                speaker.Write(update.AudioBytes);
            }
            if (!String.IsNullOrEmpty(update.AudioTranscript))
            {
                console.Write(update.AudioTranscript);
            }
        });

        updatesReceiver.Run();
        console.EndSession();

#if DEBUG
        long finishMs = updatesReceiver.CancelAndStopAll();
        if (finishMs >= 0)
        {
            Console.WriteLine($" * Info: It took {finishMs} ms to close session.");
        }
        else
        {
            Console.WriteLine(" * Error: Failed to finish session. Device tasks still running.");
        }
#endif
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
        Console.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        Console.WriteLine($" * Using Entra token-based authentication (AZURE_OPENAI_USE_ENTRA)");
        Console.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
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
        Console.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        Console.WriteLine($" * Using API key (AZURE_OPENAI_API_KEY): {aoaiApiKey[..5]}**");
        Console.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForOpenAIWithKey(string oaiApiKey)
    {
        string oaiEndpoint = "https://api.openai.com/v1";
        Console.WriteLine($" * Connecting to OpenAI endpoint (OPENAI_ENDPOINT): {oaiEndpoint}");
        Console.WriteLine($" * Using API key (OPENAI_API_KEY): {oaiApiKey[..5]}**");

        OpenAIClient aoaiClient = new(new ApiKeyCredential(oaiApiKey));
        return aoaiClient.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
    }
}
