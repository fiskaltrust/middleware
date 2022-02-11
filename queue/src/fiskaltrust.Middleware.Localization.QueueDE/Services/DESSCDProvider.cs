using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public class DESSCDProvider : IDESSCDProvider
    {
        private const string SCU_TIMEOUT_KEY = "scu-timeout-ms";
        private const string SCU_MAX_RETRIES_KEY = "scu-max-retries";

        private readonly ILogger<DESSCDProvider> _logger;
        private readonly IClientFactory<IDESSCD> _clientFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreRegister = new SemaphoreSlim(1, 1);

        private IDESSCD _instance;

        public IDESSCD Instance
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

        public DESSCDProvider(ILogger<DESSCDProvider> logger, IClientFactory<IDESSCD> clientFactory, IConfigurationRepository configurationRepository, MiddlewareConfiguration middlewareConfiguration)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _configurationRepository = configurationRepository;
            _middlewareConfiguration = middlewareConfiguration;
        }

        public async Task RegisterCurrentScuAsync()
        {
            try
            {
                await _semaphoreRegister.WaitAsync().ConfigureAwait(false);
                var queueDE = await _configurationRepository.GetQueueDEAsync(_middlewareConfiguration.QueueId).ConfigureAwait(false);

                if (!queueDE.ftSignaturCreationUnitDEId.HasValue)
                {
                    _instance = null;
                    return;
                }

                var signaturCreationUnitDE = await _configurationRepository.GetSignaturCreationUnitDEAsync(queueDE.ftSignaturCreationUnitDEId.Value).ConfigureAwait(false);
                var uri = GetUriForSignaturCreationUnit(signaturCreationUnitDE);
                var config = new ClientConfiguration
                {
                    Url = uri.ToString(),
                    UrlType = uri.Scheme
                };

                if (_middlewareConfiguration.Configuration.ContainsKey(SCU_TIMEOUT_KEY))
                {
                    config.Timeout = TimeSpan.FromMilliseconds(long.Parse(_middlewareConfiguration.Configuration[SCU_TIMEOUT_KEY].ToString()));
                }
                if (_middlewareConfiguration.Configuration.ContainsKey(SCU_MAX_RETRIES_KEY))
                {
                    config.RetryCount = int.Parse(_middlewareConfiguration.Configuration[SCU_MAX_RETRIES_KEY].ToString());
                }

                _instance = _clientFactory.CreateClient(config);
                try
                {
                    var tseInfo = await _instance.GetTseInfoAsync().ConfigureAwait(false);
                    signaturCreationUnitDE.TseInfoJson = JsonConvert.SerializeObject(tseInfo);
                    await _configurationRepository.InsertOrUpdateSignaturCreationUnitDEAsync(signaturCreationUnitDE).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to updated status of SCU (Url: {ScuUrl}, Id: {ScuId}). Will try again later...", config.Url, queueDE.ftSignaturCreationUnitDEId.Value);
                }
            }
            finally
            {
                _semaphoreRegister.Release();
            }
        }

        private static Uri GetUriForSignaturCreationUnit(ftSignaturCreationUnitDE signaturCreationUnitDE)
        {
            var url = signaturCreationUnitDE.Url;
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
