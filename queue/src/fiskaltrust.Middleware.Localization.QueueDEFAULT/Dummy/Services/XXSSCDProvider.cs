using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy.Services
{
    // The XXSSCDProvider class is responsible for managing the Signature Creation Units for the specific market "XX" (replace with the actual market name).
    /// It provides functionality to register, retrieve, and manage the SCU instances for the given market, ensuring thread-safe access.
    public class XXSSCDProvider : IXXSSCDProvider
    {

        private readonly IClientFactory<object> _clientFactory;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreRegister = new SemaphoreSlim(1, 1);

        private object _instance;

        public object Instance
        {
            get
            {
                try
                {
                    _semaphoreInstance.Wait();
                    if (_instance == null)
                    {
                        RegisterCurrentScuAsync().Wait();
                    }

                    return _instance;
                }
                finally
                {
                    _semaphoreInstance.Release();
                }
            }
        }

        public XXSSCDProvider(IClientFactory<object> clientFactory, MiddlewareConfiguration middlewareConfiguration)
        {
            _clientFactory = clientFactory;
            _middlewareConfiguration = middlewareConfiguration;
        }

        public async Task RegisterCurrentScuAsync()
        {
            try
            {
                await _semaphoreRegister.WaitAsync().ConfigureAwait(false);
                var ftSignaturCreationUnitXX = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(_middlewareConfiguration.Configuration["init_ftSignaturCreationUnitXX"].ToString());

                var uri = GetUriForSignaturCreationUnit(ftSignaturCreationUnitXX.FirstOrDefault()["Url"]);
                var config = new ClientConfiguration
                {
                    Url = uri.ToString(),
                    UrlType = uri.Scheme
                };

                _instance = _clientFactory.CreateClient(config);
            }
            finally
            {
                _semaphoreRegister.Release();
            }
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
