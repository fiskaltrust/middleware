using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueES.Externals.ifpos;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.Services
{
    public class ESSSCDProvider: IESSSCDProvider
    {

        private readonly IClientFactory<IESSSCD> _clientFactory;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreRegister = new SemaphoreSlim(1, 1);

        private IESSSCD _instance;

        public IESSSCD Instance
        {
            get
            {
                try
                {
                    _semaphoreInstance.Wait();
                    return _instance;
                }
                finally
                {
                    _semaphoreInstance.Release();
                }
            }
        }

        public ESSSCDProvider(IClientFactory<IESSSCD> clientFactory, MiddlewareConfiguration middlewareConfiguration)
        {
            _clientFactory = clientFactory;
            _middlewareConfiguration = middlewareConfiguration;
        }

        private static Uri GetUriForSignaturCreationUnit(string url)
        {

            try
            {
                var urls = JsonConvert.DeserializeObject<string[]>(url);
                var grpcUrl = urls.FirstOrDefault(x => x.StartsWith("grpc://"));
                url = grpcUrl ?? urls.First();
            }
            catch { }

            return new Uri(url);
        }
    }
}
