using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Server;

[McpServerToolType]
public static class ManagedLabsKustoTool
{
    [McpServerTool(Name = "generate-kusto-query"), Description("Generates a KQL query to using the given table information.")]
    public static async Task<string> GenerateResourceQueryAsync(
        NL2KQLClientService nl2kql,
        ILoggerFactory loggerFactory,
        [Description("Prompt to generate the KQL query. The prompt should be a natural language description of the query you want to generate.")]
        QueryParameters parameters)
    {
        try
        {
            var response = await nl2kql.GenerateQueryAsync(parameters.Category, parameters.Table, parameters.Prompt);
            return response;
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger(nameof(ManagedLabsKustoTool)).LogError(ex, "Error encountered when generating query");
            throw;
        }
    }

    [McpServerTool(Name = "execute-kusto-query"), Description("Generates and runs a KQL query against the given table. Returns results in Json format or Csv format, depending on the OutputType parameter.")]
    public static async Task<string> RunResourceQueryAsync(
        NL2KQLClientService nl2kql,
        KustoService kustoService,
        ResourceService resourceService,
        ILoggerFactory loggerFactory,
        [Description("Parameters for generating the KQL query")]
        RunQueryParameters parameters)
    {
        var logger = loggerFactory.CreateLogger(nameof(ManagedLabsKustoTool));
        try
        {
            var query = await nl2kql.GenerateQueryAsync(parameters.Category, parameters.Table, parameters.Prompt);
            query = StripCodeBlock(query);

            try
            {
                return await ExecuteQueryAsync(kustoService, resourceService, parameters, logger, query);
            }
            catch (Exception ex)
            {
                throw new QueryExecutionException($"An error occurred when executing query:\n\n{query}\n\n{ex.Message}", query, ex);
            }
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger(nameof(ManagedLabsKustoTool)).LogError(ex, "Error encountered when generating query");
            throw;
        }
    }

    private static async Task<string> ExecuteQueryAsync(
        KustoService kustoService,
        ResourceService resourceService,
        RunQueryParameters parameters,
        ILogger logger,
        string query)
    {
        switch (parameters.OutputType)
        {
            case RunQueryOutputType.Json:
                return await kustoService.RunQueryAsync(parameters.Category, parameters.Table, query);
            case RunQueryOutputType.Csv:
                // Generate csv contents
                var csv = await kustoService.GenerateCsvAsync(parameters.Category, parameters.Table, query);

                // Write contents to temporary file
                var tempFilePath = Path.GetTempFileName();
                tempFilePath = Path.ChangeExtension(tempFilePath, ".csv");

                logger.LogInformation("Writing CSV to temporary file: {TempFilePath}", tempFilePath);

                await File.WriteAllTextAsync(tempFilePath, csv);

                // Store the resource in the resource service and return it
                Dictionary<string, string> properties = new()
                {
                    { "Query", query },
                };

                var resource = new QueryResource(properties)
                {
                    Name = Path.GetFileName(tempFilePath),
                    Uri = tempFilePath,
                    MimeType = "text/csv",
                    Size = new FileInfo(tempFilePath).Length,
                    Description = "A CSV file in a temporary location created using provided query"
                };
                resourceService.AddResource(resource);
                return JsonSerializer.Serialize(resource);
            default:
                throw new ArgumentOutOfRangeException(nameof(parameters), parameters.OutputType, "Invalid output type specified.");
        }
    }

    public record QueryResource(
        [Description("Additional properties for the resource")]
        Dictionary<string, string> Properties) : Resource;

    [Description("Output type for the query results")]
    public enum RunQueryOutputType
    {
        Json,
        Csv
    }

    [Description("Parameters for generating a KQL query using natural language")]
    public record QueryParameters(
        [Description("Name of the table to run the query against.")]
        string Table,

        [Description("Category the table exists within.")]
        string Category,

        [Description("Prompt to generate the KQL query. The prompt should be a natural language description of the query you want to generate.")]
        string Prompt);

    [Description("Parameters for running a KQL query using natural language")]
    public record RunQueryParameters(
        string Table,
        string Category,
        string Prompt,
        [Description("Output type for the query results")]
        RunQueryOutputType OutputType) : QueryParameters(Table, Category, Prompt);

    private static string StripCodeBlock(string query)
    {
        if (query.StartsWith("```"))
        {
            query = query[3..];
            if (query.EndsWith("```"))
            {
                query = query[..^3];
            }
        }

        return query;
    }
}
