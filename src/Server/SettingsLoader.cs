using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Server;

public class SettingsLoader
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
        try
        {
            return deserializer.Deserialize<Settings>(contents);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to deserialize settings file: {filePath}. Error: {ex.Message}", ex);
        }
    }
}
