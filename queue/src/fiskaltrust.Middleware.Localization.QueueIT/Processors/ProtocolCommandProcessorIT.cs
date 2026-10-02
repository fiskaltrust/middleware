using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

/// <summary>
/// Protocol receipts are stored; two of them reach the RT device: the non-fiscal print and the copy of an
/// existing document.
/// </summary>
public class ProtocolCommandProcessorIT(IITSSCDProvider sscd) : IProtocolCommandProcessor
{
    private readonly IITSSCDProvider _sscd = sscd;

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request)
    {
        if (!request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsIT.NonFiscalPrint))
        {
            return await ITFallBackOperations.NoOp(request);
        }
        return await SubmitAsync(request);
    }

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => ITFallBackOperations.NotSupported(request, "Pay");

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
    {
        if (!ReceiptReferences.TryAddReferenceSignatures(request.ReceiptRequest, request.ReceiptResponse, required: true))
        {
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }
        return await SubmitAsync(request);
    }

    private async Task<ProcessCommandResponse> SubmitAsync(ProcessCommandRequest request)
    {
        try
        {
            var result = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse
            }).ConfigureAwait(false);
            return new ProcessCommandResponse(result.ReceiptResponse, []);
        }
        catch (Exception ex)
        {
            request.ReceiptResponse.SetReceiptResponseError(ex.Message);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }
    }
}
