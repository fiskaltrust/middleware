using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Storage.Base.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

/// <summary>
/// Writes the "Queue started" action journal when a v2 queue is created,
/// equivalent to CreateConfigurationActionJournalAsync in the v1 QueueBootstrapper.
/// </summary>
public static class QueueStartActionJournal
{
    public static void Create(MiddlewareConfiguration middlewareConfiguration, IStorageProvider storageProvider, ILogger logger)
    {
        try
        {
            CreateAsync(middlewareConfiguration, storageProvider).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Diagnostic entry only; must not prevent the queue from starting.
            logger.LogWarning(ex, "Could not create the 'Queue started' action journal for queue {QueueId}.", middlewareConfiguration.QueueId);
        }
    }

    public static async Task CreateAsync(MiddlewareConfiguration middlewareConfiguration, IStorageProvider storageProvider)
    {
        var configuration = new Dictionary<string, object?>
        {
            { "MachineName", Environment.MachineName },
            { "ProcessArchitecture", RuntimeInformation.ProcessArchitecture.ToString() },
            { "OSArchitecture", RuntimeInformation.OSArchitecture.ToString() },
            { "OSDescription", RuntimeInformation.OSDescription },
            { "...", "redacted" }
        };

        if (middlewareConfiguration.Configuration?.TryGetValue("configuration", out var configurationJson) == true && configurationJson is not null)
        {
            try
            {
                var cashboxConfiguration = JsonNode.Parse(configurationJson.ToString()!)!;
                var activeQueue = cashboxConfiguration["ftQueues"]!.AsArray()
                    .First(x => Guid.TryParse(x?["Id"]?.ToString(), out var id) && id == middlewareConfiguration.QueueId)!;
                var scu = cashboxConfiguration["ftSignaturCreationDevices"]?.AsArray().FirstOrDefault();

                configuration["QueueVersion"] = activeQueue["Version"]?.ToString();
                configuration["QueueUrl"] = activeQueue["Url"]?.AsArray().Select(x => x?.ToString()).ToArray();

                configuration["ScuVersion"] = scu?["Version"]?.ToString();
                configuration["ScuUrl"] = scu?["Url"]?.AsArray().Select(x => x?.ToString()).ToArray();

                configuration["CashboxConfigurationTimestamp"] = cashboxConfiguration["TimeStamp"]?.GetValue<long>();

                configuration["LauncherEnvironment"] = middlewareConfiguration.LauncherEnvironment;
                configuration["LauncherVersion"] = middlewareConfiguration.LauncherVersion;
            }
            catch { }
        }

        var actionJournal = new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(),
            ftQueueId = middlewareConfiguration.QueueId,
            ftQueueItemId = Guid.Empty,
            Moment = DateTime.UtcNow,
            Priority = 0,
            Type = nameof(MiddlewareConfiguration),
            Message = "Queue started",
            // Same shape as the v1 MiddlewareConfiguration so existing consumers can read it
            DataJson = JsonConvert.SerializeObject(new
            {
                middlewareConfiguration.CashBoxId,
                middlewareConfiguration.QueueId,
                middlewareConfiguration.IsSandbox,
                middlewareConfiguration.ServiceFolder,
                Configuration = configuration,
                middlewareConfiguration.ProcessingVersion,
                middlewareConfiguration.AssemblyName,
                middlewareConfiguration.AssemblyVersion,
                middlewareConfiguration.LauncherEnvironment,
                middlewareConfiguration.PreviewFeatures,
                middlewareConfiguration.LauncherVersion
            }),
            TimeStamp = DateTime.UtcNow.Ticks
        };

        await (await storageProvider.CreateMiddlewareActionJournalRepository()).InsertAsync(actionJournal).ConfigureAwait(false);
    }
}
