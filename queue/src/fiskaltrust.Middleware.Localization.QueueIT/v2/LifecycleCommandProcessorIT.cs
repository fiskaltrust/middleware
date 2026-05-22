using System.Text.Json;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Scu;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2;

public class LifecycleCommandProcessorIT : ILifecycleCommandProcessor
{
    private readonly IITSSCD _itSSCD;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ftQueueIT _queueIT;

    public LifecycleCommandProcessorIT(
        IITSSCD itSSCD,
        IConfigurationRepository configurationRepository,
        ftQueueIT queueIT)
    {
        _itSSCD = itSSCD;
        _configurationRepository = configurationRepository;
        _queueIT = queueIT;
    }

    public async Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request)
    {
        var queueItemId = request.ReceiptResponse.ftQueueItemID;
        var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(_queueIT.ftSignaturCreationUnitITId!.Value).ConfigureAwait(false);
        var deviceInfo = await _itSSCD.GetRTInfoAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(scu.InfoJson))
        {
            scu.InfoJson = JsonSerializer.Serialize(deviceInfo);
            await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
        }

        var signature = SignaturItemFactory.CreateInitialOperationSignature(_queueIT, deviceInfo);
        var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(request.queue, queueItemId, _queueIT, request.ReceiptRequest);

        var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
        var result = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
        V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, result.ReceiptResponse);
        if (request.ReceiptResponse.ftState.IsState(State.Error))
        {
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        request.queue.StartMoment = DateTime.UtcNow;
        await _configurationRepository.InsertOrUpdateQueueAsync(request.queue).ConfigureAwait(false);

        var signatures = new List<SignatureItem> { signature };
        signatures.AddRange(request.ReceiptResponse.ftSignatures);
        request.ReceiptResponse.ftSignatures = signatures;

        return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal> { actionJournal });
    }

    public async Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request)
    {
        var queueItemId = request.ReceiptResponse.ftQueueItemID;

        var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
        var result = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
        V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, result.ReceiptResponse);
        if (request.ReceiptResponse.ftState.IsState(State.Error))
        {
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        request.queue.StopMoment = DateTime.UtcNow;
        await _configurationRepository.InsertOrUpdateQueueAsync(request.queue).ConfigureAwait(false);

        var signatureItem = SignaturItemFactory.CreateOutOfOperationSignature(_queueIT);
        var actionJournal = ftActionJournalFactory.CreateOutOfOperationActionJournal(request.queue, queueItemId, _queueIT, request.ReceiptRequest);
        var signatures = new List<SignatureItem> { signatureItem };
        signatures.AddRange(request.ReceiptResponse.ftSignatures);
        request.ReceiptResponse.ftSignatures = signatures;

        return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal> { actionJournal });
    }

    public Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
}
