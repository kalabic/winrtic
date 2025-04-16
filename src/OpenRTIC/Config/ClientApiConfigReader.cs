using OpenRTIC.BasicDevices;
using System.Text.Json.Nodes;

namespace OpenRTIC.Config;

public class ClientApiConfigReader : ClientApiConfig
{
    static public ClientApiConfigReader FromFileOrEnvironment(string path)
    {
        ClientApiConfigReader options = new ClientApiConfigReader();
        options.fromFileOrEnvironment(path);
        return options;
    }

    static public ClientApiConfigReader FromFile(string path)
    {
        ClientApiConfigReader options = new ClientApiConfigReader();
        options.fromFileJson(path);
        return options;
    }

    private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

    static public JsonNode? GetRootJsonNode(string filePath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return null;
            }

            ReadOnlySpan<byte> jsonReadOnlySpan = File.ReadAllBytes(filePath);
            // Read past the UTF-8 BOM bytes if a BOM exists.
            if (jsonReadOnlySpan.StartsWith(Utf8Bom))
            {
                jsonReadOnlySpan = jsonReadOnlySpan.Slice(Utf8Bom.Length);
            }

            return JsonNode.Parse(jsonReadOnlySpan);
        }
        catch (Exception ex)
        {
            DeviceNotifications.Error($" * Exception while parsing {filePath}: {ex.Message}");
            return null;
        }
    }

    public EndpointType fromFileOrEnvironment(string path)
    {
        fromFileJson(path);
        if (_type == EndpointType.IncompleteOptions)
        {
            fromEnvironment();
        }

        return _type;
    }

    public EndpointType fromFileJson(string path)
    {
        var rootNode = ClientApiConfigReader.GetRootJsonNode(path);
        if (rootNode is not null)
        {
            ValueParser.AssertReadNodeStringParam(rootNode, "AZURE_OPENAI_ENDPOINT", (value) => _aoaiEndpoint = value);
            ValueParser.AssertReadNodeBoolParam(rootNode, "AZURE_OPENAI_USE_ENTRA", (value) => _aoaiUseEntra = value);
            ValueParser.AssertReadNodeStringParam(rootNode, "AZURE_OPENAI_DEPLOYMENT", (value) => _aoaiDeployment = value);
            ValueParser.AssertReadNodeStringParam(rootNode, "AZURE_OPENAI_API_KEY", (value) => _aoaiApiKey = value);
            ValueParser.AssertReadNodeStringParam(rootNode, "OPENAI_API_KEY", (value) => _oaiApiKey = value);
        }
        return updateType(ConfigSource.ApiOptionsFromFile, path);
    }
}
