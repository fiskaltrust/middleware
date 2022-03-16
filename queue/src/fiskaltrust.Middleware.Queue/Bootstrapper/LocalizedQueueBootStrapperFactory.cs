using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Localization.QueueDE;
using fiskaltrust.Middleware.Localization.QueueME;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.Bootstrapper
{
    public static class LocalizedQueueBootStrapperFactory
    {
        public static ILocalizedQueueBootstrapper GetBootstrapperForLocalizedQueue(Guid queueId, Dictionary<string, object> configuration)
        {
            var countyCode = GetQueueLocalization(queueId, configuration);
            return countyCode switch
            {
                "AT" => throw new NotImplementedException("AT IS NOT IMPLEMENTED"),
                "DE" => new QueueDEBootstrapper(),
                "FR" => throw new NotImplementedException("FR IS NOT IMPLEMENTED"),
                "ME" => new QueueMEBootstrapper(),
                _ => throw new ArgumentException($"unkown countryCode: {countyCode}"),
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
