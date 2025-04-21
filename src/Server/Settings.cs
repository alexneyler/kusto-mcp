using System.Diagnostics.CodeAnalysis;

namespace Server;

public class Settings
{
    public required Model Model { get; set; }

    public required List<KustoSettings> Kusto { get; set; }

    public bool TryGetKustoSettings(string category, string name, [NotNullWhen(true)] out KustoSettings? settings)
    {
        settings = Kusto.FirstOrDefault(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return settings != null;
    }
}

public class Model
{
    public required string Endpoint { get; set; }

    public required string Deployment { get; set; }

    public string? Key { get; set; }
}

public class KustoSettings
{
    public required string Name { get; set; }

    public required string Category { get; set; }

    public required string Database { get; set; }

    public required string Endpoint { get; set; }

    public required string Table { get; set; }

    public required List<KustoPrompt> Prompts { get; set; }
}

public class KustoPrompt
{
    public required KustoPromptType Type { get; set; }

    public required string Content { get; set; }
}

public enum KustoPromptType
{
    System,
    User,
    Assistant
}
