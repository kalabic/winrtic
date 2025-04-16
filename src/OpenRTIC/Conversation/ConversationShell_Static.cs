using OpenAI.RealtimeConversation;
using OpenRTIC.BasicDevices.RTIC;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002

public partial class ConversationShell : IConversationSessionInfo
{
    /// <summary>
    /// Does not return until session is finished.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="client"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    static public ConversationShell? RunSession(RTIConsole console,
                                                RealtimeConversationClient client,
                                                ConversationSessionOptions options,
                                                CancellationToken cancellationToken)
    {
        var conversation = new ConversationShell(console, client, cancellationToken);
        console.ConnectingStarted();
        conversation.ReceiveUpdates();
        return conversation;
    }

    static public ConversationShell? RunSessionAsync(RTIConsole console,
                                                     RealtimeConversationClient client,
                                                     ConversationSessionOptions options,
                                                     CancellationToken cancellationToken)
    {
        var conversation = new ConversationShell(console, client, cancellationToken);
        console.ConnectingStarted();
        conversation.ReceiveUpdatesAsync();
        return conversation;
    }
}
