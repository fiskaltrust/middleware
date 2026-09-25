using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

/// <summary>
/// Activation and deactivation go through the RT device: the queue only starts (or stops) operating when the
/// SCU has processed the corresponding receipt.
/// </summary>
public class LifecycleCommandProcessorIT(IITSSCDProvider sscd, ILocalizedQueueStorageProvider localizedQueueStorageProvider, AsyncLazy<IConfigurationRepository> configurationRepository) : ILifecycleCommandProcessor
{
    private readonly IITSSCDProvider _sscd = sscd;
    private readonly ILocalizedQueueStorageProvider _localizedQueueStorageProvider = localizedQueueStorageProvider;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        // TODO SKE =>  We need to figure a way to retry this functionality in case we fail to do something. There are a few states that
        //              we need to take care of:
        // - SCU is not rechable => initial operation fails with EEEE_EEEE and needs to be retried by the caller
        // - SCU is reachable but fails internall => initial operation fails with EEEE_EEEE and needs to be retried by the caller
        // - SCU succeeds, but the Queue fails to receive / store the result for whatever reason => initial operation fails with EEEE_EEEE and needs to be retried by the caller but the SCU should be capable of handling that
        var (queue, receiptRequest, receiptResponse) = request;
        var configurationRepository = await _configurationRepository;
        var queueIT = await configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
        if (queueIT?.ftSignaturCreationUnitITId is null)
        {
            receiptResponse.SetReceiptResponseError(ErrorMessagesIT.NoSignaturCreationUnitAssigned(queue.ftQueueId));
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var signaturCreationUnitIT = await configurationRepository.GetSignaturCreationUnitITAsync(queueIT.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
        if (signaturCreationUnitIT is null)
        {
            receiptResponse.SetReceiptResponseError(ErrorMessagesIT.SignaturCreationUnitNotFound(queueIT.ftSignaturCreationUnitITId.Value));
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var deviceInfo = await _sscd.GetRTInfoAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(signaturCreationUnitIT.InfoJson))
        {
            signaturCreationUnitIT.InfoJson = JsonConvert.SerializeObject(deviceInfo);
            await configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(signaturCreationUnitIT).ConfigureAwait(false);
        }

        var signature = SignaturItemFactory.CreateInitialOperationSignature(queueIT, deviceInfo);
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(queueIT, receiptRequest, receiptResponse);

        var result = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
        }).ConfigureAwait(false);
        if (result.ReceiptResponse.HasFailed())
        {
            return new ProcessCommandResponse(result.ReceiptResponse, []);
        }

        await _localizedQueueStorageProvider.ActivateQueueAsync().ConfigureAwait(false);
        receiptResponse.ftSignatures = [signature, .. result.ReceiptResponse.ftSignatures];
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var queueIT = await (await _configurationRepository).GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);

        var result = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
        }).ConfigureAwait(false);
        if (result.ReceiptResponse.HasFailed())
        {
            return new ProcessCommandResponse(result.ReceiptResponse, []);
        }

        await _localizedQueueStorageProvider.DeactivateQueueAsync().ConfigureAwait(false);
        var signature = SignaturItemFactory.CreateOutOfOperationSignature(queueIT);
        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(queueIT, receiptRequest, receiptResponse);
        receiptResponse.ftSignatures = [signature, .. result.ReceiptResponse.ftSignatures];
        receiptResponse.MarkAsDisabled();
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }

    public async Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);
}
