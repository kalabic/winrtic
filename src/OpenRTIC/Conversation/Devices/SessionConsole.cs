using OpenAI.RealtimeConversation;
using OpenRTIC.BasicDevices.RTIC;
using OpenRTIC.MiniTaskLib;

namespace OpenRTIC.Conversation.Devices;

#pragma warning disable OPENAI002

public class SessionConsole
{
    private IRTIConsole _console;

    public const int DEFAULT_CONSOLE_BUFFER_SIZE = 1024;

    public SessionConsole(IRTIConsole console)
    {
        this._console = console;
    }

    public void ConnectSessionEvents(EventCollection sessionEvents)
    {
        //
        // ConversationSessionStartedUpdate
        //
        sessionEvents.Connect<ConversationSessionStartedUpdate>(false, (_, update) =>
        {
            // Notify console output that session has started.
            _console.SessionStarted(" *\n * Session started (Ctrl-C to finish)\n *");
        });

        //
        // SendAudioTaskFinished
        //
        sessionEvents.Connect<SendAudioTaskFinished>(false, (_, update) =>
        {
            _console.SessionFinished("Audio input stream is stopped\nSESSION FINISHED");
        });

        //
        // ConversationResponseStartedUpdate
        //
        sessionEvents.Connect<ConversationResponseStartedUpdate>(false, (_, update) =>
        {
            _console.ItemStarted(update.EventId);
        });

        //
        // ConversationResponseFinishedUpdate
        //
        sessionEvents.Connect<ConversationResponseFinishedUpdate>(false, (_, update) =>
        {
            _console.ItemFinished();
        });

        //
        // ConversationInputTranscriptionFinishedUpdate
        //
        sessionEvents.Connect<ConversationInputTranscriptionFinishedUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.Transcript))
            {
                _console.WriteLine(RTIOut.User, update.Transcript);
            }
        });

        //
        // ConversationInputTranscriptionFailedUpdate
        //
        sessionEvents.Connect<ConversationInputTranscriptionFailedUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.ErrorMessage))
            {
                _console.WriteLine(RTIOut.User, update.ErrorMessage);
            }
        });

        //
        // ConversationItemStreamingPartDeltaUpdate
        //
        sessionEvents.Connect<ConversationItemStreamingPartDeltaUpdate>(false, (_, update) =>
        {
            if (!String.IsNullOrEmpty(update.AudioTranscript))
            {
                _console.Write(RTIOut.Agent, update.AudioTranscript);
            }
        });
    }

    public void WriteError(string message)
    {
        _console.WriteLine(message);
    }
}
