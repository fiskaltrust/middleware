using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        protected override IQueueRepository IQueueRepository => _iQueueRepository;
        private readonly IQueueRepository _iQueueRepository;

        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly IITSSCD _client;

        public InitialOperationReceiptCommand(IQueueRepository iQeueRepository,  IITSSCDProvider itIsscdProvider, ILogger<InitialOperationReceiptCommand> logger, IConfigurationRepository configurationRepository, SignatureItemFactoryIT signatureItemFactoryIT) : base(logger, configurationRepository)
        {
            _client = itIsscdProvider.Instance;
            _configurationRepository = configurationRepository;
            _signatureItemFactoryIT = signatureItemFactoryIT;
            _iQueueRepository = iQeueRepository;
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
            var signatureItem = _signatureItemFactoryIT.CreateInitialOperationSignature($"Queue-ID: {queue.ftQueueId} Serial-Nr: {deviceInfo.SerialNumber}");
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
