using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public static class MiddlewareConfigurationFactory
{
    public static MiddlewareConfiguration CreateMiddlewareConfiguration(Guid id, Dictionary<string, object> configuration)
    {
        return new MiddlewareConfiguration
        {
            CashBoxId = GetQueueCashbox(id, configuration),
            QueueId = id,
            IsSandbox = configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
            ServiceFolder = configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
            Configuration = configuration
        };
    }

    private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

    private static Guid GetQueueCashbox(Guid queueId, Dictionary<string, object> configuration)
    {
        var key = "init_ftQueue";
        if (configuration.ContainsKey(key))
        {
            var queues = JsonConvert.DeserializeObject<List<ftQueue>>(configuration[key]!.ToString()!);
            return queues.Where(q => q.ftQueueId == queueId).First().ftCashBoxId;
        }
        else
        {
            throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
        }
    }
}
