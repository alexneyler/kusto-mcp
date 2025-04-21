using ModelContextProtocol;

namespace Server;

internal class QueryExecutionException(string message, string query, Exception? innerException = null)
    : McpException(message, innerException, McpErrorCode.InternalError)
{
    public string Query
    {
        get;
    } = query;
}
