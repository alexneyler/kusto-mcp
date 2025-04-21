using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using System.Diagnostics;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Server;

public partial class SettingsLoader
{
    private readonly Lazy<Settings> settings;

    public SettingsLoader(IConfiguration configuration)
    {
        var filePath = configuration["settings"];
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("Settings file path cannot be null or empty.", nameof(configuration));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Settings file not found: {filePath}");
        }

        this.settings = new Lazy<Settings>(() => LoadSettings(filePath));
    }

    public Settings Get()
    {
        return this.settings.Value;
    }

    private static Settings LoadSettings(string filePath)
    {
        var json = File.ReadAllText(filePath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var contents = File.ReadAllText(filePath);
        contents = ReplaceEnvironmentVariables(contents);

        try
        {
            return deserializer.Deserialize<Settings>(contents);
        }
        catch (Exception ex)
        {
            throw new McpException($"Failed to deserialize settings file: {filePath}. Error: {ex.Message}", ex, McpErrorCode.InternalError);
        }
    }

    private static string ReplaceEnvironmentVariables(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var regex = EnvironmentVariableRegex();
        var result = regex.Replace(input, match =>
        {
            var variableName = match.Groups[1].Value;
            var value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrEmpty(value))
            {
                throw new McpException($"Environment variable '{variableName}' is not set.", McpErrorCode.InternalError);
            }

            return value;
        });

        return result;
    }

    [GeneratedRegex(@"\$\{\{\s*(.*?)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex EnvironmentVariableRegex();
}
