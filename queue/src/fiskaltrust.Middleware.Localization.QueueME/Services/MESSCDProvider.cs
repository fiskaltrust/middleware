using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.Services
{
    public class MESSCDProvider : IMESSCDProvider
    {

        private readonly IClientFactory<IMESSCD> _clientFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreRegister = new SemaphoreSlim(1, 1);

        private IMESSCD _instance;

        public IMESSCD Instance
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

        public MESSCDProvider(IClientFactory<IMESSCD> clientFactory, IConfigurationRepository configurationRepository, MiddlewareConfiguration middlewareConfiguration)
        {
            _clientFactory = clientFactory;
            _configurationRepository = configurationRepository;
            _middlewareConfiguration = middlewareConfiguration;
        }

        public async Task RegisterCurrentScuAsync()
        {
            try
            {
                await _semaphoreRegister.WaitAsync().ConfigureAwait(false);
                var ftSignaturCreationUnitME = JsonConvert.DeserializeObject<List<ftSignaturCreationUnitDE>>(_middlewareConfiguration.Configuration["init_ftSignaturCreationUnitDE"].ToString());

                var uri = GetUriForSignaturCreationUnit(ftSignaturCreationUnitME.FirstOrDefault().Url);
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
