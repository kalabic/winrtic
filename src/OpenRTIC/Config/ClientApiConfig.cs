namespace OpenRTIC.Config;

public enum EndpointType
{
    IncompleteOptions,
    AzureOpenAIWithEntra,
    AzureOpenAIWithKey,
    OpenAIWithKey,
}

public class ClientApiConfig
{
    static public ClientApiConfig FromEnvironment()
    {
        var config = new ClientApiConfig();
        config.fromEnvironment();
        return config;
    }


    public EndpointType Type { get { return _type; } }

    public string AOAIEndpoint { get { return (_aoaiEndpoint is not null) ? _aoaiEndpoint : ""; } }

    public string AOAIDeployment { get { return (_aoaiDeployment is not null) ? _aoaiDeployment : ""; } }

    public string AOAIApiKey { get { return (_aoaiApiKey is not null) ? _aoaiApiKey : ""; } }

    public string OAIApiKey { get { return (_oaiApiKey is not null) ? _oaiApiKey : ""; } }

    protected string? _aoaiEndpoint;
    protected bool    _aoaiUseEntra = false;
    protected string? _aoaiDeployment;
    protected string? _aoaiApiKey;
    protected string? _oaiApiKey;
    protected EndpointType _type = EndpointType.IncompleteOptions;

    public EndpointType fromEnvironment()
    {
        _aoaiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? aoaiUseEntra = Environment.GetEnvironmentVariable("AZURE_OPENAI_USE_ENTRA");
        if (!bool.TryParse(aoaiUseEntra, out _aoaiUseEntra)) _aoaiUseEntra = false;
        _aoaiDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        _aoaiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        _oaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        return updateType();
    }

    protected EndpointType updateType()
    {
        if (!String.IsNullOrEmpty(_aoaiEndpoint) && _aoaiUseEntra)
        {
            _type = EndpointType.AzureOpenAIWithEntra;
        }
        else if (!String.IsNullOrEmpty(_aoaiEndpoint) && !String.IsNullOrEmpty(_aoaiApiKey))
        {
            _type = EndpointType.AzureOpenAIWithKey;
        }
        else if (!String.IsNullOrEmpty(_aoaiEndpoint))
        {
            // AZURE_OPENAI_ENDPOINT configured without AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY.
            _type = EndpointType.IncompleteOptions;
        }
        else if (!String.IsNullOrEmpty(_oaiApiKey))
        {
            _type = EndpointType.OpenAIWithKey;
        }
        else
        {
            // No environment configuration present. Please provide one of:
            // - AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY
            // - OPENAI_API_KEY
            _type = EndpointType.IncompleteOptions;
        }
        return _type;
    }
}

