using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.RealtimeConversation;
using OpenAI;
using System.ClientModel;
using OpenRTIC.BasicDevices;

namespace OpenRTIC.Config;

#pragma warning disable OPENAI002

public class ConfiguredClient
{
    public const string DEFAULT_CONVERSATIONAPI_FILENAME = "winrtic_api.conf";

    public static RealtimeConversationClient? FromOptions(ClientApiConfig options)
    {
        switch (options.Type)
        {
            case EndpointType.AzureOpenAIWithEntra:
                return ForAzureOpenAIWithEntra(options.AOAIEndpoint, options.AOAIDeployment);

            case EndpointType.AzureOpenAIWithKey:
                return ForAzureOpenAIWithKey(options.AOAIEndpoint, options.AOAIDeployment, options.AOAIApiKey);

            case EndpointType.OpenAIWithKey:
                return ForOpenAIWithKey(options.OAIApiKey);
        }

        DeviceNotifications.Error(
                    $"Incomplete or missing '" + DEFAULT_CONVERSATIONAPI_FILENAME + "' or environment configuration.Please provide one of:\n"
                    + " - AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY\n"
                    + " - OPENAI_API_KEY");
        return null;
    }

    private static RealtimeConversationClient ForAzureOpenAIWithEntra(string aoaiEndpoint, string? aoaiDeployment)
    {
        DeviceNotifications.Info($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        DeviceNotifications.Info($" * Using Entra token-based authentication (AZURE_OPENAI_USE_ENTRA)");
        DeviceNotifications.Info(string.IsNullOrEmpty(aoaiDeployment)
                                 ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
                                 : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new DefaultAzureCredential());
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient ForAzureOpenAIWithKey(string aoaiEndpoint, string? aoaiDeployment, string aoaiApiKey)
    {
        DeviceNotifications.Info($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        DeviceNotifications.Info($" * Using API key (AZURE_OPENAI_API_KEY): {aoaiApiKey[..5]}**");
        DeviceNotifications.Info(string.IsNullOrEmpty(aoaiDeployment)
                                 ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
                                 : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient ForOpenAIWithKey(string oaiApiKey)
    {
        string oaiEndpoint = "https://api.openai.com/v1";
        DeviceNotifications.Info($" * Connecting to OpenAI endpoint (OPENAI_ENDPOINT): {oaiEndpoint}");
        DeviceNotifications.Info($" * Using API key (OPENAI_API_KEY): {oaiApiKey[..5]}**");

        OpenAIClient aoaiClient = new(new ApiKeyCredential(oaiApiKey));
        return aoaiClient.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
    }
}
