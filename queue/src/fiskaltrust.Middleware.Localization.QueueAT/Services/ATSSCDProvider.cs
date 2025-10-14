using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;

namespace fiskaltrust.Middleware.Localization.QueueAT.Services
{
    public class ATSSCDProvider : IATSSCDProvider
    {
        private const int DEFAULT_TIMEOUT_SEC = 15;

        private readonly ILogger<ATSSCDProvider> _logger;
        private readonly IClientFactory<IATSSCD> _clientFactory;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly QueueATConfiguration _queueATConfiguration;

        private readonly SemaphoreSlim _semaphoreInstance = new SemaphoreSlim(1, 1);
        private List<(ftSignaturCreationUnitAT scu, IATSSCD client)> _instances;
        private readonly Task<List<(ftSignaturCreationUnitAT scu, IATSSCD client)>> _instanceTask;
        private int _currentlyActiveInstance = 0;

        public ATSSCDProvider(ILogger<ATSSCDProvider> logger, IClientFactory<IATSSCD> clientFactory, IConfigurationRepository configurationRepository, QueueATConfiguration queueATConfiguration)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _configurationRepository = configurationRepository;
            _queueATConfiguration = queueATConfiguration;
            _instanceTask = Task.Run(async () =>
            {
                return await GetScusFromConfigurationAsync().ToListAsync();
            });

        }

        public async Task<int> GetCurrentlyActiveInstanceIndexAsync()
        {
            try
            {
                _semaphoreInstance.Wait();
                if (_instances == null)
                {
                    _instances = await _instanceTask;
                }

                return _currentlyActiveInstance;
            }
            finally
            {
                _semaphoreInstance.Release();
            }
        }

        public async Task<List<(ftSignaturCreationUnitAT scu, IATSSCD sscd)>> GetAllInstances()
        {
            try
            {
                _semaphoreInstance.Wait();
                if (_instances == null)
                {
                    _instances = await _instanceTask;
                }

                return _instances.ToList();
            }
            finally
            {
                _semaphoreInstance.Release();
            }
        }

        public int SwitchToNextScu()
        {
            if (_instances == null || !_instances.Any())
            {
                _currentlyActiveInstance = 0;
            }

            if (_currentlyActiveInstance < _instances.Count - 1)
            {
                _currentlyActiveInstance++;
            }
            else
            {
                _currentlyActiveInstance = 0;
            }

            return _currentlyActiveInstance;
        }

        public void SwitchToFirstScu() => _currentlyActiveInstance = 0;

        //private async IAsyncEnumerable<(ftSignaturCreationUnitAT scu, IATSSCD client)> GetScusFromConfigurationAsync()
        //{
        //    var scus = (await _configurationRepository.GetSignaturCreationUnitATListAsync()).ToList();
        //    foreach (var scu in scus.OrderBy(x => x.Mode & 0xff))
        //    {
        //        // SCU is disabled
        //        if ((scu.Mode & 0xFF) >= 99)
        //        {
        //            continue;
        //        }

        //        var timeoutSec = DEFAULT_TIMEOUT_SEC;
        //        if ((scu.Mode & 0xFF00) > 0)
        //        {
        //            // Timeouts are encoded in the third and fourth byte of the Mode property :)
        //            timeoutSec = (scu.Mode & 0xFF00) >> 8;
        //        }

        //        var uri = GetUriForSignaturCreationUnit(scu);

        //        var client = _clientFactory.CreateClient(new ClientConfiguration
        //        {
        //            Timeout = TimeSpan.FromSeconds(timeoutSec),
        //            RetryCount = _queueATConfiguration.ScuMaxRetries,
        //            Url = uri.ToString(),
        //            UrlType = uri.Scheme
        //        });

        //        await UpdateConfigurationAsync(scu, client);

        //        yield return (scu, client);
        //    }
        //}
        private async IAsyncEnumerable<(ftSignaturCreationUnitAT scu, IATSSCD client)> GetScusFromConfigurationAsync()
        {
            var scus = (await _configurationRepository.GetSignaturCreationUnitATListAsync()).ToList();

            var tasks = scus
                .OrderBy(x => x.Mode & 0xff)
                .Where(scu => (scu.Mode & 0xFF) < 99)
                .Select(async scu =>
                {
                    // Timeouts are encoded in the third and fourth byte of the Mode property :)
                    var timeoutSec = ((scu.Mode & 0xFF00) > 0) ? (scu.Mode & 0xFF00) >> 8 : DEFAULT_TIMEOUT_SEC;

                    var uri = GetUriForSignaturCreationUnit(scu);
                    var client = _clientFactory.CreateClient(new ClientConfiguration
                    {
                        Timeout = TimeSpan.FromSeconds(timeoutSec),
                        RetryCount = _queueATConfiguration.ScuMaxRetries,
                        Url = uri.ToString(),
                        UrlType = uri.Scheme
                    });

                    await UpdateConfigurationAsync(scu, client);
                    return (scu, client);
                });

            foreach (var task in await Task.WhenAll(tasks))
            {
                yield return task;
            }
        }


        private async Task UpdateConfigurationAsync(ftSignaturCreationUnitAT scu, IATSSCD client)
        {
            try
            {
#pragma warning disable CS0618
                var certResponse = client.Certificate();
#pragma warning restore
                var certificate = new X509CertificateParser().ReadCertificate(certResponse);
                var certificateSerial = certificate?.SerialNumber?.ToString(16);

#pragma warning disable CS0618
                var zdaResponse = client.ZDA();
#pragma warning restore
                scu.ZDA = zdaResponse;
                scu.SN = certificateSerial != null ? $"0x{certificateSerial}" : null;
                scu.CertificateBase64 = Convert.ToBase64String(certificate.GetEncoded());

                await _configurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(scu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update SCU {ScuId} in database because an error occured.", scu.ftSignaturCreationUnitATId);
            }
        }

        private static Uri GetUriForSignaturCreationUnit(ftSignaturCreationUnitAT signaturCreationUnitDE)
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
