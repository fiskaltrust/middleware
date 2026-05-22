using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Scu;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2;

public class DailyOperationsCommandProcessorIT : IDailyOperationsCommandProcessor
{
    private readonly IITSSCD _itSSCD;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IJournalITRepository _journalITRepository;
    private readonly ftQueueIT _queueIT;

    public DailyOperationsCommandProcessorIT(
        IITSSCD itSSCD,
        IConfigurationRepository configurationRepository,
        IJournalITRepository journalITRepository,
        ftQueueIT queueIT)
    {
        _itSSCD = itSSCD;
        _configurationRepository = configurationRepository;
        _journalITRepository = journalITRepository;
        _queueIT = queueIT;
    }

    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request)
    {
        if (_queueIT.SSCDFailCount != 0)
        {
            _queueIT.SSCDFailCount = 0;
            _queueIT.SSCDFailMoment = null;
            _queueIT.SSCDFailQueueItemId = null;
            await _configurationRepository.InsertOrUpdateQueueITAsync(_queueIT).ConfigureAwait(false);
        }

        var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
        var establishConnection = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
        V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, establishConnection.ReceiptResponse);
        return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
    }

    public Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
        => RunClosingAsync(request, ftActionJournalFactory.CreateDailyClosingActionJournal);

    public Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
        => RunClosingAsync(request, ftActionJournalFactory.CreateMonthlyClosingActionJournal);

    public Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request)
        => RunClosingAsync(request, ftActionJournalFactory.CreateYearlyClosingClosingActionJournal);

    private async Task<ProcessCommandResponse> RunClosingAsync(
        ProcessCommandRequest request,
        Func<ftQueue, Guid, V2.ReceiptRequest, ftActionJournal> journalFactory)
    {
        var queueItemId = request.ReceiptResponse.ftQueueItemID;
        var actionJournal = journalFactory(request.queue, queueItemId, request.ReceiptRequest);

        var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
        var result = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
        V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, result.ReceiptResponse);
        if (request.ReceiptResponse.ftState.IsState(State.Error))
        {
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        var zNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber)!.Data;
        request.ReceiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";

        var journalIT = ftJournalITFactory.CreateFrom(queueItemId, request.ReceiptRequest.cbReceiptReference, _queueIT, new ScuResponse
        {
            ftReceiptCase = (long) request.ReceiptRequest.ftReceiptCase,
            ZRepNumber = long.Parse(zNumber),
        });
        await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);

        return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal> { actionJournal });
    }
}
