using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

namespace Server;

public class NL2KQLClientService
{
    private static readonly SystemChatMessage s_systemChatMessage
        = new("""
            You are a helpful assistant that translates natural language queries into KQL queries. You will receive a system message indicating the structure of a given KQL table, followed by a few examples showing expected outputs."
            """);

    private readonly IReadOnlyDictionary<string, List<ChatMessage>> _seedMessages;
    private readonly ChatClient _chatClient;
    private readonly AzureOpenAIClient _azOpenAiClient;

    public NL2KQLClientService(SettingsLoader settingsLoader)
    {
        var settings = settingsLoader.Get();
        _seedMessages = LoadTable(settings);
        _azOpenAiClient = new(
            new Uri(settings.Model.Endpoint),
            new DefaultAzureCredential());
        _chatClient = _azOpenAiClient.GetChatClient(settings.Model.Deployment);
    }

    public async Task<string> GenerateQueryAsync(string category, string table, string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }
        if (string.IsNullOrEmpty(table))
        {
            throw new ArgumentException("Table cannot be null or empty.", nameof(table));
        }

        var seedMessages = _seedMessages.GetValueOrDefault(GetKey(category, table))
            ?? throw new ArgumentException($"The table '{table}' in category '{category}' is not supported. Supported tables: {string.Join(';', _seedMessages.Keys)}");

        var result = await _chatClient.CompleteChatAsync(
        [
            s_systemChatMessage,
            ..seedMessages,
            new UserChatMessage(prompt)
        ]);

        return result.Value.Content[0].Text;
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
        _ => throw new NotSupportedException($"Unsupported prompt type: {prompt.Type}"),
    };
}
