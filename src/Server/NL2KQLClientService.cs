using Azure.AI.OpenAI;
using Azure.Identity;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenAI.Chat;
using System.ClientModel;

namespace Server;

public class NL2KQLClientService
{
    private static readonly SystemChatMessage s_systemChatMessage
        = new("""
            You are a helpful assistant that translates natural language queries into KQL queries. You will receive a system message indicating the structure of a given KQL table, followed by a few examples showing expected outputs."
            """);

    private readonly IReadOnlyDictionary<string, List<ChatMessage>> seedMessages;
    private readonly Lazy<ChatClient> chatClientLazy;
    private readonly IMcpServer server;

    public NL2KQLClientService(SettingsLoader settingsLoader, IMcpServer server)
    {
        var settings = settingsLoader.Get();
        this.seedMessages = LoadTable(settings);
        this.chatClientLazy = new Lazy<ChatClient>(() =>
        {
            AzureOpenAIClient azOpenAiClient = !string.IsNullOrEmpty(settings.Model.Key)
                ? new(new Uri(settings.Model.Endpoint), new ApiKeyCredential(settings.Model.Key))
                : new(new Uri(settings.Model.Endpoint), new DefaultAzureCredential());
            return azOpenAiClient.GetChatClient(settings.Model.Deployment);
        });
        this.server = server;
    }

    public async Task<string> GenerateQueryAsync(string category, string table, string prompt, bool sample = false)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }
        if (string.IsNullOrEmpty(table))
        {
            throw new ArgumentException("Table cannot be null or empty.", nameof(table));
        }

        var seedMessages = this.seedMessages.GetValueOrDefault(GetKey(category, table))
            ?? throw new McpException(
                $"The table '{table}' in category '{category}' is not supported. Supported tables: {string.Join(';', this.seedMessages.Keys)}",
                McpErrorCode.InvalidParams);

        // Sample from the server -- note that this is not yet supported by vs code
        if (sample)
        {
            var samplingParams = CreateMessageRequestParams(s_systemChatMessage, seedMessages, new UserChatMessage(prompt));
            var samplingResult = await this.server.RequestSamplingAsync(samplingParams, default);
            return samplingResult.Content.Text ?? string.Empty;
        }

        var result = await this.chatClientLazy.Value.CompleteChatAsync(
        [
            s_systemChatMessage,
            ..seedMessages,
            new UserChatMessage(prompt)
        ]);

        return result.Value.Content[0].Text;
    }

    private static CreateMessageRequestParams CreateMessageRequestParams(
        SystemChatMessage systemMessage,
        IEnumerable<ChatMessage> messages,
        UserChatMessage userMessage)
    {
        var chatMessages = new List<ChatMessage>();
        chatMessages.AddRange(messages.Where(m => m is not SystemChatMessage));
        chatMessages.Add(userMessage);

        var systemMessageText = string.Join("\n---\n", [systemMessage.Content.First().Text, .. chatMessages.Where(m => m is SystemChatMessage).Select(m => m.Content.First().Text)]);
        return new CreateMessageRequestParams
        {
            Messages = [.. chatMessages.Select(m => new SamplingMessage
            {
                Role = m switch
                {
                    UserChatMessage ucm => Role.User,
                    AssistantChatMessage acm => Role.Assistant,
                    _ => throw new McpException($"Cannot convert message of type {m.GetType()} to a sampling message role.", McpErrorCode.InternalError),
                },
                Content = new Content()
                {
                    Type = "text",
                    Text = m.Content.First().Text,
                }
            })],
            SystemPrompt = systemMessageText,
            IncludeContext = ContextInclusion.AllServers,
            Temperature = 0.7f,
        };
    }

    private static Dictionary<string, List<ChatMessage>> LoadTable(Settings settings)
    {
        var table = new Dictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kustoSettings in settings.Kusto)
        {
            List<ChatMessage> messages = new(kustoSettings.Prompts.Count);

            foreach (var prompt in kustoSettings.Prompts)
            {
                messages.Add(ToChatMessage(prompt));
            }

            table[GetKey(kustoSettings)] = messages;
        }

        return table;
    }

    private static string GetKey(KustoSettings settings) => GetKey(settings.Category, settings.Name);

    private static string GetKey(string category, string tableName) => $"Category: {category}, Table: {tableName}";

    private static ChatMessage ToChatMessage(KustoPrompt prompt) => prompt.Type switch
    {
        KustoPromptType.System => new SystemChatMessage(prompt.Content),
        KustoPromptType.User => new UserChatMessage(prompt.Content),
        KustoPromptType.Assistant => new AssistantChatMessage(prompt.Content),
        _ => throw new McpException($"Unsupported prompt type: {prompt.Type}", McpErrorCode.InternalError),
    };
}
