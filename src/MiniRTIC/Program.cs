#define RUN_SYNC

using OpenAI.RealtimeConversation;
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

        // Read client API options from environment variables and nothing else.
        var config = ConversationOptions.FromEnvironment();
        if (config._client is null)
        {
            RTIConsole.WriteLine("Error: Failed to read environment options.");
            return;
        }

        //
        // Create devices, register to be notified about some receiverQueueEvents and run!
        //

        var speaker = new SpeakerAudioStream(ConversationSessionConfig.AudioFormat, GetCancellationToken());
        var microphone = new MicrophoneAudioStream(ConversationSessionConfig.AudioFormat, GetCancellationToken());
        var updatesReceiver = new ConversationUpdatesReceiverTask(GetCancellationToken());
        updatesReceiver.ConfigureWith(config, microphone);

        //
        // A collection of receiverQueueEvents to listen on, will be invoked from a task that is not used
        // for fetching conversation updates.
        //
        var receiverEvents = updatesReceiver.ReceiverEvents;
        var receiverQueueEvents = updatesReceiver.ReceiverQueueEvents;

        //
        // FailedToConnect
        //
        receiverEvents.Connect<FailedToConnect>((_, update) =>
        {
            RTIConsole.ConnectingFailed(update._message);
        });

        //
        // ConversationSessionStartedUpdate
        //
        receiverQueueEvents.Connect<ConversationSessionStartedUpdate>(false, (_, update) =>
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
        receiverQueueEvents.Connect<SendAudioTaskFinished>(false, (_, update) =>
        {
            RTIConsole.SessionFinished("Audio input stream is stopped\nSESSION FINISHED");
        });

        //
        // ConversationInputSpeechStartedUpdate
        //
        receiverQueueEvents.Connect<ConversationInputSpeechStartedUpdate>(false, (_, update) =>
        {
            // Ratio speaker volume while user is speaking.
            speaker.Volume = 0.3f;
        });

        //
        // ConversationInputSpeechFinishedUpdate
        //
        receiverQueueEvents.Connect<ConversationInputSpeechFinishedUpdate>(false, (_, update) =>
        {
            speaker.Volume = 1.0f;
        });

        //
        // ConversationResponseStartedUpdate
        //
        receiverQueueEvents.Connect<ConversationResponseStartedUpdate>(false, (_, update) =>
        {
            speaker.ClearBuffer();
            RTIConsole.ItemStarted(update.EventId);
        });

        //
        // ConversationResponseFinishedUpdate
        //
        receiverQueueEvents.Connect<ConversationResponseFinishedUpdate>(false, (_, update) =>
        {
            RTIConsole.ItemFinished();
        });

        //
        // ConversationInputTranscriptionFinishedUpdate
        //
        receiverQueueEvents.Connect<ConversationInputTranscriptionFinishedUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.Transcript))
            {
                RTIConsole.WriteLine(RTIOut.User, update.Transcript);
            }
        });

        //
        // ConversationInputTranscriptionFailedUpdate
        //
        receiverQueueEvents.Connect<ConversationInputTranscriptionFailedUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.ErrorMessage))
            {
                RTIConsole.WriteLine(RTIOut.User, update.ErrorMessage);
            }
        });

        //
        // ConversationItemStreamingPartDeltaUpdate
        //
        receiverQueueEvents.Connect<ConversationItemStreamingPartDeltaUpdate>(false, (_, update) =>
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
}
