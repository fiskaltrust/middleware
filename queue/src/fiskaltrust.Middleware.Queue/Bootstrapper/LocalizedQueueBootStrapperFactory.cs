using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT;
using fiskaltrust.Middleware.Localization.QueueDE;
using fiskaltrust.Middleware.Localization.QueueDEFAULT;
using fiskaltrust.Middleware.Localization.QueueES;
using fiskaltrust.Middleware.Localization.QueueIT;
using fiskaltrust.Middleware.Localization.QueueFR;
using fiskaltrust.Middleware.Localization.QueueME;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Queue.Bootstrapper
{
    public static class LocalizedQueueBootStrapperFactory
    {
        public static ILocalizedQueueBootstrapper GetBootstrapperForLocalizedQueue(Guid queueId, MiddlewareConfiguration middlewareConfiguration)
        {
            var countyCode = GetQueueLocalization(queueId, middlewareConfiguration.Configuration);
            return countyCode switch
            {
                "AT" => new QueueATBootstrapper(),
                "DE" => new QueueDEBootstrapper(),
                "ES" => new QueueESBootstrapper(),
                "FR" => middlewareConfiguration.PreviewFeatures.TryGetValue("queue-fr", out var val) && val ? new QueueFRBootstrapper() : throw new NotImplementedException("The French Queue is not yet implemented in this version."),
                "IT" => new QueueITBootstrapper(),
                "ME" => new QueueMeBootstrapper(),
                "DEFAULT" => new QueueDEFAULTBootstrapper(middlewareConfiguration),
                _ => throw new ArgumentException($"Unkown country code: {countyCode}"),

            };
        }

        public static string GetQueueLocalization(Guid queueId, Dictionary<string, object> configuration)
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
