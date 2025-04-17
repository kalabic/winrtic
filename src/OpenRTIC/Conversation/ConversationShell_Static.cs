using OpenAI.RealtimeConversation;
using OpenRTIC.BasicDevices.RTIC;
using OpenRTIC.Config;

namespace OpenRTIC.Conversation;

#pragma warning disable OPENAI002

public partial class ConversationShell
{
    /// <summary>
    /// Does not return until session is finished.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="client"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    static public ConversationShell? RunSession(IRTIConsole console,
                                                RealtimeConversationClient client,
                                                ConversationSessionOptions options,
                                                CancellationToken cancellationToken)
    {
        var conversation = new ConversationShell(console, client, cancellationToken);
        console.ConnectingStarted();
        conversation.ReceiveUpdates();
        return conversation;
    }

    static public ConversationShell? RunSession(IRTIConsole console,
                                                ConversationOptions options,
                                                CancellationToken cancellationToken)
    {
        var conversation = new ConversationShell(console, options, cancellationToken);
        console.ConnectingStarted();
        conversation.ReceiveUpdates();
        return conversation;
    }

    static public ConversationShell? RunSessionAsync(IRTIConsole console,
                                                     RealtimeConversationClient client,
                                                     ConversationSessionOptions options,
                                                     CancellationToken cancellationToken)
    {
        var conversation = new ConversationShell(console, client, cancellationToken);
        console.ConnectingStarted();
        conversation.ReceiveUpdatesAsync();
        return conversation;
    }

    static public ConversationShell? RunSessionAsync(IRTIConsole console,
                                                     ConversationOptions options,
                                                     CancellationToken cancellationToken)
    {
        var conversation = new ConversationShell(console, options, cancellationToken);
        console.ConnectingStarted();
        conversation.ReceiveUpdatesAsync();
        return conversation;
    }
}
