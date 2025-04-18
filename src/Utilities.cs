namespace Server;

internal static class Utilities
{
    public static bool IsBinary(string? mimeType)
    {
        if (mimeType is null)
        {
            return false;
        }

        return mimeType.StartsWith("application/") || mimeType.StartsWith("image/") || mimeType.StartsWith("video/");
    }
}
