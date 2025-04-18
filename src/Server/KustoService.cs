using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;

namespace Server;

public class KustoService(SettingsLoader settingsLoader, ILogger<KustoService> logger)
{
    private readonly Settings settings = settingsLoader.Get();

    public async Task<string> RunQueryAsync(string category, string table, string query)
    {
        using var reader = await RunQueryCoreAsync(category, table, query);
        return reader.ToJsonString();
    }

    public async Task<string> GenerateCsvAsync(string category, string table, string query)
    {
        using var reader = await RunQueryCoreAsync(category, table, query);
        var sb = new StringBuilder();
        var dt = new DataTable();
        dt.Load(reader);
        foreach (DataColumn column in dt.Columns)
        {
            sb.Append(column.ColumnName + ",");
        }

        sb.AppendLine();

        foreach (DataRow row in dt.Rows)
        {
            foreach (var item in row.ItemArray)
            {
                sb.Append(item + ",");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<IDataReader> RunQueryCoreAsync(string category, string table, string query)
    {
        if (!settings.TryGetKustoSettings(category, table, out var kustoSettings))
        {
            throw new ArgumentException($"No cluster information found for table {table} in category {category}. Supported tables: {string.Join(';', settings.Kusto.Select(s => $"Category: {s.Category}, Table: {s.Name}"))}");
        }

        logger.LogInformation("Running query against database \"{Database}\": \n{Query}", kustoSettings.Database, query);

        var kcsb = new KustoConnectionStringBuilder(kustoSettings.Endpoint)
            .WithAadUserPromptAuthentication();
        using var client = KustoClientFactory.CreateCslQueryProvider(kcsb);

        try
        {
            return await client.ExecuteQueryAsync(kustoSettings.Database, query, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error encountered while executing query");
            throw;
        }
    }
}
