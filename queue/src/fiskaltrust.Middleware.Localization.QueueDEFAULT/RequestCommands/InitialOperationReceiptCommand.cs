using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Services;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryDEFAULT _signatureItemFactoryDefault;
        private readonly IITSSCD _client;

        public InitialOperationReceiptCommand(ICountrySpecificSettings countrySpecificQueueSettings,  IITSSCDProvider itIsscdProvider, ILogger<InitialOperationReceiptCommand> logger, IConfigurationRepository configurationRepository, SignatureItemFactoryDEFAULT signatureItemFactoryDefault) : base(countrySpecificQueueSettings, logger, configurationRepository)
        {
            _client = itIsscdProvider.Instance;
            _configurationRepository = configurationRepository;
            _signatureItemFactoryDefault = signatureItemFactoryDefault;
            _countrySpecificQueueRepository = countrySpecificQueueSettings.CountrySpecificQueueRepository;

        }

        protected override async Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var queueIt = await _configurationRepository.GetQueueITAsync(queue.ftQueueId);
            var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIt.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
            var deviceInfo = await _client.GetDeviceInfoAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(scu.InfoJson ))
            {
                scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
            }
            var signatureItem = _signatureItemFactoryDefault.CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId} Serial-Nr: {deviceInfo.SerialNumber}");
            var notification = new ActivateQueueSCU
            {
                CashBoxId = Guid.Parse(request.ftCashBoxID),
                QueueId = queueItem.ftQueueId,
                Moment = DateTime.UtcNow,
                SCUId = queueIt.ftSignaturCreationUnitITId.GetValueOrDefault(),
                IsStartReceipt = true,
                Version = "V0",
            };
            var actionJournal = CreateActionJournal(queue.ftQueueId, $"{request.ftReceiptCase:X}-{nameof(ActivateQueueSCU)}",
                queueItem.ftQueueItemId, $"Initial-Operation receipt. Queue-ID: {queue.ftQueueId}", JsonConvert.SerializeObject(notification));

            return (actionJournal, signatureItem);
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
    }
}
