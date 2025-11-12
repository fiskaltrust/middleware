using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.QueueSynchronizer;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.Bootstrapper
{
    public class QueueBootstrapper
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly Guid _activeQueueId;
        private readonly string _assemblyName;
        private readonly Version _assemblyVersion;
        private readonly string _processingVersion;

        public QueueBootstrapper(Guid queueId, Dictionary<string, object> configuration, Type assemblyType)
        {
            _configuration = configuration;
            _activeQueueId = queueId;
            (_assemblyName, _assemblyVersion, _processingVersion) = GetVersion(assemblyType);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                CashBoxId = GetQueueCashbox(_activeQueueId, _configuration),
                QueueId = _activeQueueId,
                IsSandbox = _configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
                ServiceFolder = _configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
                ProcessingVersion = _processingVersion,
                AssemblyVersion = _assemblyVersion,
                AssemblyName = _assemblyName,
                Configuration = _configuration,
                PreviewFeatures = GetPreviewFeatures(_configuration),
                // Key handling is based on this condition to ensure that we are handling it case insensitive
                LauncherEnvironment =  _configuration.FirstOrDefault(x => x.Key?.ToLower() == "launcherenvironment").Value?.ToString() ?? null,
                AllowUnsafeScuSwitch = _configuration.TryGetValue("AllowUnsafeScuSwitch", out var allowUnsafeScuSwitch) && bool.TryParse(allowUnsafeScuSwitch.ToString(), out var allowUnsafeScuSwitchBool) && allowUnsafeScuSwitchBool,
            };

            services.AddSingleton(sp =>
            {
                CreateConfigurationActionJournalAsync(middlewareConfiguration, sp.GetRequiredService<IMiddlewareQueueItemRepository>(), sp.GetRequiredService<IMiddlewareActionJournalRepository>()).Wait();
                return middlewareConfiguration;
            });

            services.AddScoped<ICryptoHelper, CryptoHelper>();
            services.AddScoped<SignProcessor>();
            services.AddScoped<ISignProcessor>(x => new LocalQueueSynchronizationDecorator(x.GetRequiredService<SignProcessor>(), x.GetRequiredService<ILogger<LocalQueueSynchronizationDecorator>>()));
            services.AddScoped<IJournalProcessor, JournalProcessor>();
            services.AddScoped<IPOS, Queue>();
            var businessLogicFactoryBoostrapper = LocalizedQueueBootStrapperFactory.GetBootstrapperForLocalizedQueue(_activeQueueId, middlewareConfiguration);
            businessLogicFactoryBoostrapper.ConfigureServices(services);
        }

        private static async Task CreateConfigurationActionJournalAsync(MiddlewareConfiguration middlewareConfiguration, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository)
        {
            if((middlewareConfiguration.LauncherEnvironment != LauncherEnvironments.Cloud) && MigrationHelper.IsMigrationInProgress(queueItemRepository, actionJournalRepository))
            {
                return;
            }

            var configuration = new Dictionary<string, object>
            {
                { "MachineName", Environment.MachineName },
                { "ProcessArchitecture", RuntimeInformation.ProcessArchitecture.ToString() },
                { "OSArchitecture", RuntimeInformation.OSArchitecture.ToString() },
                { "OSDescription", RuntimeInformation.OSDescription.ToString() },
                { "...", "redacted"}
            };

            static List<Dictionary<string, object>> DeserializeListOfDictionaries(string json)
                => JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();

            static string GetStr(IDictionary<string, object> map, params string[] names)
            {
                foreach (var n in names)
                {
                    if (map.TryGetValue(n, out var v))
                    {
                        var s = v?.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                    }
                }
                return null;
            }

            static Guid? GetGuid(IDictionary<string, object> map, params string[] names)
            {
                foreach (var n in names)
                {
                    if (map.TryGetValue(n, out var v) && Guid.TryParse(v?.ToString(), out var g)) return g;
                }
                return null;
            }

            try
            {
                if (middlewareConfiguration.Configuration.TryGetValue("init_ftQueue", out var rawQueueJson)
                    && rawQueueJson is string queueJson
                    && !string.IsNullOrWhiteSpace(queueJson))
                {
                    var queues = DeserializeListOfDictionaries(queueJson);
                    var current = queues.FirstOrDefault(m =>
                    {
                        var id = GetGuid(m, "ftQueueId");
                        return id.HasValue && id.Value == middlewareConfiguration.QueueId;
                    });

                    if (current != null)
                    {
                        var queueVersion = GetStr(current, "Version", "version", "AssemblyVersion");
                        if (!string.IsNullOrWhiteSpace(queueVersion))
                            configuration["QueueVersion"] = queueVersion;

                        var queueUrl = GetStr(current, "Url", "url", "Endpoint", "endpoint");
                        if (!string.IsNullOrWhiteSpace(queueUrl))
                            configuration["QueueUrl"] = queueUrl;
                    }
                }
            }
            catch { }

            try
            {
                var scuVersions = new List<object>();
                var scuUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var entry in middlewareConfiguration.Configuration)
                {
                    if (!entry.Key.StartsWith("init_ftSignaturCreationUnit", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (entry.Value is not string scuJson || string.IsNullOrWhiteSpace(scuJson))
                        continue;

                    var items = DeserializeListOfDictionaries(scuJson);
                    foreach (var m in items)
                    {
                        var id = GetStr(m, "ftSignaturCreationUnitId", "Id", "id");
                        var version = GetStr(m, "Version", "version", "AssemblyVersion", "DriverVersion");

                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
                            scuVersions.Add(new { Id = id, Version = version });

                        var url = GetStr(m, "Url", "url", "Endpoint", "endpoint");
                        if (!string.IsNullOrWhiteSpace(url))
                            scuUrls.Add(url);
                    }
                }

                if (scuVersions.Count > 0)
                    configuration["ScuVersions"] = scuVersions;

                if (scuUrls.Count > 0)
                    configuration["ScuUrls"] = scuUrls.ToList();
            }
            catch { }

            try
            {
                configuration["AssemblyInformation"] = new
                {
                    Assembly = middlewareConfiguration.AssemblyName,
                    Version = middlewareConfiguration.AssemblyVersion,
                    ProcessingVersion = middlewareConfiguration.ProcessingVersion
                };
            }
            catch { }

            try
            {
                foreach (var key in new[] { "init_ftCashBox", "init_ftCashBoxList" })
                {
                    if (middlewareConfiguration.Configuration.TryGetValue(key, out var cbRaw)
                        && cbRaw is string cbJson
                        && !string.IsNullOrWhiteSpace(cbJson))
                    {
                        var list = DeserializeListOfDictionaries(cbJson);
                        var current = list.FirstOrDefault(m =>
                        {
                            var id = GetGuid(m, "ftCashBoxId");
                            return id.HasValue && id.Value == middlewareConfiguration.CashBoxId;
                        });

                        if (current != null)
                        {
                            var tsStr = GetStr(current, "Timestamp", "TimeStamp", "UpdatedAt");
                            if (!string.IsNullOrWhiteSpace(tsStr))
                            {
                                if (DateTime.TryParse(tsStr, out var dt))
                                    configuration["CashboxConfigurationTimestamp"] = dt.ToUniversalTime().Ticks;
                                else
                                    configuration["CashboxConfigurationTimestamp"] = tsStr;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }

            var actionJournal = new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = middlewareConfiguration.QueueId,
                ftQueueItemId = Guid.Empty,
                Moment = DateTime.UtcNow,
                Priority = 0,
                Type = nameof(MiddlewareConfiguration),
                Message = "Queue started",
                DataJson = JsonConvert.SerializeObject(new MiddlewareConfiguration
                {
                    CashBoxId = middlewareConfiguration.CashBoxId,
                    QueueId = middlewareConfiguration.QueueId,
                    IsSandbox = middlewareConfiguration.IsSandbox,
                    ServiceFolder = middlewareConfiguration.ServiceFolder,
                    Configuration = configuration,
                    PreviewFeatures = middlewareConfiguration.PreviewFeatures,
                }),
                TimeStamp = DateTime.UtcNow.Ticks
            };

            await actionJournalRepository.InsertAsync(actionJournal);
        }

        private static Guid GetQueueCashbox(Guid queueId, Dictionary<string, object> configuration)
        {
            var key = "init_ftQueue";
            if (configuration.ContainsKey(key))
            {
                var queues = JsonConvert.DeserializeObject<List<ftQueue>>(configuration[key].ToString());
                return queues.Where(q => q.ftQueueId == queueId).FirstOrDefault().ftCashBoxId;
            }
            else
            {
                throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
            }
        }

        private static Dictionary<string, bool> GetPreviewFeatures(Dictionary<string, object> configuration)
        {
            var key = "previewFeatures";
            try
            {
                return configuration.ContainsKey(key)
                ? JsonConvert.DeserializeObject<Dictionary<string, bool>>(configuration[key].ToString())
                : new Dictionary<string, bool>();
            }
            catch
            {
                return new();
            }
        }

        private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

        private (string name, Version version, string processingVersion) GetVersion(Type assemblyType)
        {
            var assemblyName = assemblyType.Assembly.GetName();
            var fileAttribute = assemblyType.Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var processingVersion = assemblyType.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var version = Version.TryParse(fileAttribute, out var result)
                ? new Version(result.Major, result.Minor, result.Build, 0)
                : new Version(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build, 0);
            assemblyName.Version = version;
            return (assemblyName.FullName, version, processingVersion);
        }
    }
}