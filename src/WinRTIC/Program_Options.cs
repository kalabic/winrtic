using OpenRTIC.Config;
using System.CommandLine;


public partial class Program
{
    protected class CommandLineArguments
    {
        public FileInfo? apiFile = null;
        public FileInfo? sessionFile = null;
    }

    protected static ConversationOptions? GetProgramOptionsWithCommandLineArguments(string[] args)
    {
        var sessionFileArgument = new Argument<FileInfo>("session file")
        {
            Description = "JSON formatted file with root object containing key/value pairs, all optional\n" +
                          "- 'Instructions' - text prompt for assistant,\n" +
                          "- 'Temperature' - decimal value between 0.6 and 1.2, (default: " + ConversationOptions.DEFAULT_TEMPERATURE + ")\n" +
                          "- 'ServerVAD' - object with optional key/value pairs. Server VAD means that the model will detect the start and end of speech based on audio volume and respond at the end of user speech.\n" +
                          "    - 'Threshold' -  Activation threshold for VAD (0.0 to 1.0). A higher threshold will require louder audio to activate the model, and might perform better in noisy environments., (default: " + ConversationOptions.DEFAULT_SERVERVAD_THRESHOLD + ")\n" +
                          "    - 'PrefixPadding' - Amount of audio to include before the VAD detected speech. Value in miliseconds between 0 and 2000, (default: " + ConversationOptions.DEFAULT_SERVERVAD_PREFIXPADDINGMS + ")\n" +
                          "    - 'SilenceDuration' - Duration of silence to detect speech stop. Value in miliseconds between 0 and 2000, (default: " + ConversationOptions.DEFAULT_SERVERVAD_SILENCEDURATIONMS + ")\n",
            Arity = ArgumentArity.ZeroOrOne,
        }.ExistingOnly();

        var apiFileOption = new Option<FileInfo>("--api")
        {
            Description = "File with API options. (OPENAI_API_KEY or AZURE options)",
            Arity = ArgumentArity.ZeroOrOne,
            AllowMultipleArgumentsPerToken = false,
        }.ExistingOnly();

        var rootCommand = new RootCommand(
            "If no arguments are given, API options will be loaded from JSON formatted '" + 
            ConfiguredClient.DEFAULT_CONVERSATIONAPI_FILENAME + "' file in current directory " +
            "or from environment variables if file doesn't exit. Please provide one of:\n" +
            " - AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY\n" +
            " - OPENAI_API_KEY\n");

        rootCommand.AddOption(apiFileOption);
        rootCommand.AddArgument(sessionFileArgument);

        var arguments = new CommandLineArguments();
        rootCommand.SetHandler( (apiFile, sessionFile) =>
        {
            arguments.apiFile = apiFile;
            arguments.sessionFile = sessionFile;
        }, apiFileOption, sessionFileArgument);
        rootCommand.Invoke(args);

        if (arguments.apiFile is null && arguments.sessionFile is null)
        {
            return null;
        }

        try
        {
            return ConversationOptions.ReadFromFile(arguments.apiFile, arguments.sessionFile);
        }
        catch (Exception ex)
        {
            return new ConversationOptions($" * Exception while parsing file options: {ex.Message}");
        }
    }
}
