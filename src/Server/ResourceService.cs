using ModelContextProtocol;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace Server;

public class ResourceService(IMcpServer mcpServer)
{
    private readonly Dictionary<string, Resource> resources = [];
    private readonly HashSet<string> subscriptions = [];

    public void AddSubscription(string uri)
    {
        this.subscriptions.Add(uri);
    }

    public void RemoveSubscription(string uri)
    {
        if (!this.subscriptions.Remove(uri))
        {
            throw new KeyNotFoundException($"Subscription with uri {uri} not found.");
        }
    }

    public void AddResource(Resource resource)
    {
        if (this.resources.ContainsKey(resource.Uri))
        {
            throw new ArgumentException($"Resource with uri {resource.Uri} already exists.");
        }

        this.resources[resource.Uri] = resource;
        _ = mcpServer.SendNotificationAsync("notifications/resources/list_changed");
    }

    public void RemoveResource(string resourceUri)
    {
        if (!this.resources.ContainsKey(resourceUri))
        {
            throw new KeyNotFoundException($"Resource with uri {resourceUri} not found.");
        }

        this.resources.Remove(resourceUri);
        _ = mcpServer.SendNotificationAsync("notifications/resources/list_changed");
    }

    public void UpdateResource(Resource resource)
    {
        if (!this.resources.ContainsKey(resource.Uri))
        {
            throw new KeyNotFoundException($"Resource with uri {resource.Uri} not found.");
        }

        this.resources[resource.Uri] = resource;

        // Notify subscribers if the resource is in their subscription list
        if (this.subscriptions.Contains(resource.Uri))
        {
            _ = mcpServer.SendNotificationAsync("notifications/resources/updated", new { resource.Uri });
        }
    }

    public Resource? GetResource(string resourceUri)
    {
        if (!this.resources.TryGetValue(resourceUri, out var resource))
        {
            return null;
        }

        return resource;
    }

    public List<Resource> List() => this.resources.Values.ToList();
}
