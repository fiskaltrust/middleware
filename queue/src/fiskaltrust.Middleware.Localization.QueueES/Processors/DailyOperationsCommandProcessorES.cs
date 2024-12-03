using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class DailyOperationsCommandProcessorES(IESSSCD sscd, IQueueStorageProvider queueStorageProvider) : IDailyOperationsCommandProcessor
{
#pragma warning disable
    private readonly IESSSCD _sscd = sscd;
    private readonly IQueueStorageProvider _queueStorageProvider = queueStorageProvider;
#pragma warning restore
    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
        switch (receiptCase)
        {
            case (int) ReceiptCases.ZeroReceipt0x2000:
                return await ZeroReceipt0x2000Async(request);
            case (int) ReceiptCases.OneReceipt0x2001:
                return await OneReceipt0x2001Async(request);
            case (int) ReceiptCases.ShiftClosing0x2010:
                return await ShiftClosing0x2010Async(request);
            case (int) ReceiptCases.DailyClosing0x2011:
                return await DailyClosing0x2011Async(request);
            case (int) ReceiptCases.MonthlyClosing0x2012:
                return await MonthlyClosing0x2012Async(request);
            case (int) ReceiptCases.YearlyClosing0x2013:
                return await YearlyClosing0x2013Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase(request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request)
    {
        var previousQueueItem = await _queueStorageProvider.LoadLastReceipt();
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem!.request)!, // handle null case?
            PreviousReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem!.response)!,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request)
    {
        var previousQueueItem = await _queueStorageProvider.LoadLastReceipt();
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem!.request)!, // handle null case?
            PreviousReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem!.response)!,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request)
    {
        var previousQueueItem = await _queueStorageProvider.LoadLastReceipt();
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem!.request)!, // handle null case?
            PreviousReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem!.response)!,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
    public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
    {
        var previousQueueItem = await _queueStorageProvider.LoadLastReceipt();
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem!.request)!, // handle null case?
            PreviousReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem!.response)!,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
    public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
    {
        var previousQueueItem = await _queueStorageProvider.LoadLastReceipt();
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem!.request)!, // handle null case?
            PreviousReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem!.response)!,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
    public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request)
    {
        var previousQueueItem = await _queueStorageProvider.LoadLastReceipt();
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem!.request)!, // handle null case?
            PreviousReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem!.response)!,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}