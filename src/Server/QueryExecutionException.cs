namespace Server;

internal class QueryExecutionException(string message, string query, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Query
    {
        get;
    } = query;
}
