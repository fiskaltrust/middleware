using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

/// <summary>
/// The zero receipt re-establishes the connection to the RT device, the closings trigger the Z report on it.
/// </summary>
public class DailyOperationsCommandProcessorIT(IITSSCDProvider sscd, AsyncLazy<IMiddlewareJournalITRepository> journalITRepository, AsyncLazy<IConfigurationRepository> configurationRepository) : IDailyOperationsCommandProcessor
{
    private readonly IITSSCDProvider _sscd = sscd;
    private readonly AsyncLazy<IMiddlewareJournalITRepository> _journalITRepository = journalITRepository;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;

    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var configurationRepository = await _configurationRepository;
        var queueIT = await configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
        if (queueIT.SSCDFailCount != 0)
        {
            queueIT.SSCDFailCount = 0;
            queueIT.SSCDFailMoment = null;
            queueIT.SSCDFailQueueItemId = null;
            await configurationRepository.InsertOrUpdateQueueITAsync(queueIT).ConfigureAwait(false);
        }

        var result = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        }).ConfigureAwait(false);
        return new ProcessCommandResponse(result.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
        => ClosingAsync(request, ftActionJournalFactory.CreateDailyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse));

    public Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
        => ClosingAsync(request, ftActionJournalFactory.CreateMonthlyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse));

    public Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request)
        => ClosingAsync(request, ftActionJournalFactory.CreateYearlyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse));

    private async Task<ProcessCommandResponse> ClosingAsync(ProcessCommandRequest request, ftActionJournal actionJournal)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var result = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        }).ConfigureAwait(false);
        if (result.ReceiptResponse.HasFailed())
        {
            return new ProcessCommandResponse(result.ReceiptResponse, []);
        }

        var zNumber = result.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTZNumber)?.Data;
        if (zNumber is null)
        {
            receiptResponse.SetReceiptResponseError(ErrorMessagesIT.MissingRTZNumber);
            return new ProcessCommandResponse(receiptResponse, []);
        }

        receiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";
        receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;

        var queueIT = await (await _configurationRepository).GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
        await (await _journalITRepository).InsertAsync(ftJournalITFactory.CreateFrom(receiptResponse, queueIT, new ScuResponse
        {
            ftReceiptCase = receiptRequest.ftReceiptCase,
            ZRepNumber = long.Parse(zNumber)
        })).ConfigureAwait(false);
        return new ProcessCommandResponse(receiptResponse, [actionJournal]);
    }
}
