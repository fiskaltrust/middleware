using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.Services
{
    public class ITSSCDProvider : IITSSCDProvider
    {
        private readonly IClientFactory<IITSSCD> _clientFactory;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly ILogger<ITSSCDProvider> _logger;
        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreRegister = new SemaphoreSlim(1, 1);

        private IITSSCD _instance;

        public IITSSCD Instance
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

        public ITSSCDProvider(IClientFactory<IITSSCD> clientFactory, MiddlewareConfiguration middlewareConfiguration, ILogger<ITSSCDProvider> logger)
        {
            _clientFactory = clientFactory;
            _middlewareConfiguration = middlewareConfiguration;
            _logger = logger;
        }

        public async Task RegisterCurrentScuAsync()
        {
            try
            {
                await _semaphoreRegister.WaitAsync().ConfigureAwait(false);
                var ftSignaturCreationUnitIT = JsonConvert.DeserializeObject<List<ftSignaturCreationUnitIT>>(_middlewareConfiguration.Configuration["init_ftSignaturCreationUnitIT"].ToString());

                var uri = GetUriForSignaturCreationUnit(ftSignaturCreationUnitIT.FirstOrDefault().Url);
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

        public async Task<bool> IsSSCDAvailable()
        {
            try
            {
                var deviceInfo = await _instance.GetRTInfoAsync().ConfigureAwait(false);
                _logger.LogDebug(JsonConvert.SerializeObject(deviceInfo));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on DeviceInfo Request.");
                return false;
            }
        }

        public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => await Instance.ProcessReceiptAsync(request).ConfigureAwait(false);
        public async Task<RTInfo> GetRTInfoAsync() => await Instance.GetRTInfoAsync().ConfigureAwait(false);
    }
}
