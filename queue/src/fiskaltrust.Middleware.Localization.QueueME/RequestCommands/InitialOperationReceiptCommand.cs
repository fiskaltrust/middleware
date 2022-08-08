using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Localization.QueueME.Factories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class InitialOperationReceiptCommand : RequestCommand
    {
        private readonly IMasterDataRepository<OutletMasterData> _outletMasterDataRepository;
        private readonly IMasterDataRepository<PosSystemMasterData> _posSystemMasterDataRepository;
        private readonly IMasterDataRepository<AccountMasterData> _accountMasterDataRepository;
        private readonly SignatureItemFactory _signatureItemFactory;

        public InitialOperationReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository,
            IMasterDataRepository<OutletMasterData> outletMasterDataRepository, IMasterDataRepository<PosSystemMasterData> posSystemMasterDataRepository, IMasterDataRepository<AccountMasterData> accountMasterDataRepository,
            QueueMEConfiguration queueMeConfiguration, SignatureItemFactory signatureItemFactory) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        {
            _outletMasterDataRepository = outletMasterDataRepository;
            _posSystemMasterDataRepository = posSystemMasterDataRepository;
            _accountMasterDataRepository = accountMasterDataRepository;
            _signatureItemFactory = signatureItemFactory;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME, bool subsequent = false)
        {
            try
            {
                if (queue.IsNew())
                {
                    // TODO: Make it possible to select the desired PosSystem and Outlet when we made this configurable via the Portal
                    var outlets = await _outletMasterDataRepository.GetAsync().ConfigureAwait(false);
                    var posSystems = await _posSystemMasterDataRepository.GetAsync().ConfigureAwait(false);
                    var accounts = await _accountMasterDataRepository.GetAsync().ConfigureAwait(false);
                    
                    var businessUnitCode = outlets.FirstOrDefault()?.LocationId ?? throw new ArgumentException("The primary outlet's LocationId needs to be set to the business unit code.");
                    var tcrSoftwareCode = posSystems.FirstOrDefault()?.Model ?? throw new ArgumentException("The primary PosSystem's Model needs to be set to the TCR software code.");
                    var tcrMaintainerCode = posSystems.FirstOrDefault()?.Brand ?? throw new ArgumentException("The primary PosSystem's Brand needs to be set to the TCR maintainer code.");
                    var issuerTin = accounts.FirstOrDefault()?.TaxId ?? throw new ArgumentException("The account's TaxId needs to be set to the issuers TIN code.");

                    var registerTCRRequest = new RegisterTcrRequest
                    {
                        BusinessUnitCode = businessUnitCode,
                        TcrSoftwareCode = tcrSoftwareCode,
                        TcrSoftwareMaintainerCode = tcrMaintainerCode,
                        InternalTcrIdentifier = queue.ftQueueId.ToString(),
                        RequestId = queueItem.ftQueueItemId,
                        TcrType = TcrType.Regular
                    };
                    if (!string.IsNullOrEmpty(posSystems.FirstOrDefault()?.Type) && Enum.TryParse<TcrType>(posSystems.FirstOrDefault()?.Type, out var tcrType))
                    {
                        registerTCRRequest.TcrType = tcrType;
                    }
                    var registerTCRResponse = await client.RegisterTcrAsync(registerTCRRequest).ConfigureAwait(false);

                    var scuMe = await ConfigurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                    scuMe.BusinessUnitCode = registerTCRRequest.BusinessUnitCode;
                    scuMe.TcrCode = registerTCRResponse.TcrCode;
                    scuMe.TcrIntId = registerTCRRequest.InternalTcrIdentifier;
                    scuMe.SoftwareCode = registerTCRRequest.TcrSoftwareCode;
                    scuMe.MaintainerCode = registerTCRRequest.TcrSoftwareMaintainerCode;
                    scuMe.ValidFrom = DateTime.UtcNow;
                    scuMe.IssuerTin = issuerTin;
                    await ConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scuMe).ConfigureAwait(false);

                    await ConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
                    queue.StartMoment = DateTime.UtcNow;

                    var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
                    receiptResponse.ftSignatures = receiptResponse.ftSignatures.Extend(_signatureItemFactory.CreateInitialOperationSignature(queue.ftQueueId, registerTCRResponse.TcrCode));
                    
                    var notification = new ActivateQueueSCU
                    {
                        CashBoxId = Guid.Parse(request.ftCashBoxID),
                        QueueId = queueItem.ftQueueId,
                        Moment = DateTime.UtcNow,
                        SCUId = queueME.ftSignaturCreationUnitMEId.GetValueOrDefault(),
                        IsStartReceipt = true,
                        Version = "V0"
                    };
                    var actionJournal = new ftActionJournal
                    {
                        ftActionJournalId = Guid.NewGuid(),
                        ftQueueId = queueItem.ftQueueId,
                        ftQueueItemId = queueItem.ftQueueItemId,
                        Moment = DateTime.UtcNow,
                        Priority = -1,
                        TimeStamp = 0,
                        Message = $"Initial-Operation receipt. TCR-Code: {registerTCRResponse.TcrCode}, Queue-ID: {queue.ftQueueId}",
                        Type = $"{0x4D45000000000003:X}-{nameof(ActivateQueueSCU)}",
                        DataJson = JsonConvert.SerializeObject(notification)
                    };
                    return new RequestCommandResponse
                    {
                        ReceiptResponse = receiptResponse,
                        ActionJournals = new List<ftActionJournal> { actionJournal }
                    };
                }
                var actionJournalEntry = new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = queue.IsDeactivated()
                        ? $"Queue {queue.ftQueueId} is de-activated, initial-operations-receipt can not be executed."
                        : $"Queue {queue.ftQueueId} is already activated, initial-operations-receipt can not be executed."
                };
                Logger.LogInformation(actionJournalEntry.Message);
                return new RequestCommandResponse
                {
                    ActionJournals = new List<ftActionJournal> { actionJournalEntry },
                    ReceiptResponse = CreateReceiptResponse(queue, request, queueItem)
                };
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "An exception occured while processing this request.");
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
                receiptResponse.SetStateToError(ErrorCode.Error, ex.Message);
                return new RequestCommandResponse { ReceiptResponse = receiptResponse };
            }
        }
        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request) => Task.FromResult(false);
    }
}
