using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE;
using fiskaltrust.Middleware.Localization.QueueFR;
using fiskaltrust.Middleware.Localization.QueueME;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.Bootstrapper
{
    public static class LocalizedQueueBootStrapperFactory
    {
        public static ILocalizedQueueBootstrapper GetBootstrapperForLocalizedQueue(Guid queueId, MiddlewareConfiguration middlewareConfiguration)
        {
            var countyCode = GetQueueLocalization(queueId, middlewareConfiguration.Configuration);
            return countyCode switch
            {
                "AT" => throw new NotImplementedException("The Austrian Queue is not yet implemented in this version."),
                "DE" => new QueueDEBootstrapper(),
                "FR" => middlewareConfiguration.PreviewFeatures.TryGetValue("queue-fr", out var val) && val ? new QueueFRBootstrapper() : throw new NotImplementedException("The French Queue is not yet implemented in this version."),
                "ME" => middlewareConfiguration.PreviewFeatures.TryGetValue("queue-me", out var val) && val ? new QueueMEBootstrapper() : throw new NotImplementedException("The Montenegran Queue is not yet implemented in this version."),
                _ => throw new ArgumentException($"Unkown country code: {countyCode}"),
            };
        }

        private static string GetQueueLocalization(Guid queueId, Dictionary<string, object> configuration)
        {
            var key = "init_ftQueue";
            if (configuration.ContainsKey(key))
            {
                var queues = JsonConvert.DeserializeObject<List<ftQueue>>(configuration[key].ToString());
                return queues.Where(q => q.ftQueueId == queueId).FirstOrDefault().CountryCode;
            }
            else
            {
                throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
            }
        }
    }
}
