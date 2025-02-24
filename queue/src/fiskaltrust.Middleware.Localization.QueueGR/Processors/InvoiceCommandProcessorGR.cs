using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Storage.GR;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class InvoiceCommandProcessorGR(IGRSSCD sscd, ftQueueGR queueGR, ftSignaturCreationUnitGR signaturCreationUnitGR) : IInvoiceCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly ftQueueGR _queueGR = queueGR;
    private readonly ftSignaturCreationUnitGR _signaturCreationUnitGR = signaturCreationUnitGR;
#pragma warning restore

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase.Case();
        switch (receiptCase)
        {
            case ReceiptCase.InvoiceUnknown0x1000:
                return await InvoiceUnknown0x1000Async(request);
            case ReceiptCase.InvoiceB2C0x1001:
                return await InvoiceB2C0x1001Async(request);
            case ReceiptCase.InvoiceB2B0x1002:
                return await InvoiceB2B0x1002Async(request);
            case ReceiptCase.InvoiceB2G0x1003:
                return await InvoiceB2G0x1003Async(request);
            case (ReceiptCase) 0x1004: // TODO
                return await InvoiceUnknown0x1000Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase((long) request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await InvoiceUnknown0x1000Async(request);
}