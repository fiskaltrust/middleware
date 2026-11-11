using System.Reflection;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2.Configuration;

public static class MiddlewareConfigurationFactory
{
    /// <param name="assemblyType">A type from the localized queue assembly (e.g. the bootstrapper), used for the assembly name and versions.</param>
    public static MiddlewareConfiguration CreateMiddlewareConfiguration(Guid id, Dictionary<string, object> configuration, Type assemblyType)
    {
        var (assemblyName, assemblyVersion, processingVersion) = GetVersion(assemblyType);
        return new MiddlewareConfiguration
        {
            CashBoxId = GetQueueCashbox(id, configuration),
            QueueId = id,
            IsSandbox = configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
            ServiceFolder = configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
            Configuration = configuration,
            ProcessingVersion = processingVersion,
            AssemblyName = assemblyName,
            AssemblyVersion = assemblyVersion,
            // Key handling is case insensitive, consistent with the v1 QueueBootstrapper
            LauncherVersion = configuration.FirstOrDefault(x => x.Key?.ToLower() == "launcherversion").Value?.ToString(),
            LauncherEnvironment = configuration.FirstOrDefault(x => x.Key?.ToLower() == "launcherenvironment").Value?.ToString()
        };
    }

    // Same logic as GetVersion in the v1 QueueBootstrapper
    private static (string name, Version version, string processingVersion) GetVersion(Type assemblyType)
    {
        var assembly = assemblyType.Assembly;
        var assemblyName = assembly.GetName();
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        var processingVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        var version = Version.TryParse(fileVersion, out var result)
            ? new Version(result.Major, result.Minor, result.Build, 0)
            : new Version(assemblyName.Version!.Major, assemblyName.Version.Minor, assemblyName.Version.Build, 0);
        assemblyName.Version = version;
        return (assemblyName.FullName, version, processingVersion);
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
