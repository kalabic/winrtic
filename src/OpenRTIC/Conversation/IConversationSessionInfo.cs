namespace OpenRTIC.Conversation;

public interface IConversationSessionInfo
{
    long GetPlaybackBufferMs();

    long GetTimeSinceSpeechStartedMs();

    string GetTranscriptionBuffer();
}
