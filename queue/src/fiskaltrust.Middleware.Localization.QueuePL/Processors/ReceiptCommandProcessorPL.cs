using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// Passes receipts through to the register SCU after validating the protocol constraints the
/// device would reject anyway. Tax-class immutability (item name ↔ PTU slot) is enforced by the
/// register's product database and therefore stays device-side.
/// </summary>
public class ReceiptCommandProcessorPL(IPLSSCD sscd) : IReceiptCommandProcessor
{
    private readonly IPLSSCD _sscd = sscd;

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => SubmitAsync(request);

    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => PLFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => PLFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => SubmitAsync(request);

    public Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => PLFallBackOperations.NotSupported(request, "DeliveryNote");

    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => PLFallBackOperations.NotSupported(request, "TableCheck");

    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request) => PLFallBackOperations.NotSupported(request, "ProForma");

    private async Task<ProcessCommandResponse> SubmitAsync(ProcessCommandRequest request)
    {
        if (HasMixedSaleAndReturn(request.ReceiptRequest))
        {
            request.ReceiptResponse.SetReceiptResponseError("A Polish fiscal document must not mix sale and return positions — the register protocol processes returns as separate non-fiscal documents. Send the return as its own receipt (flag Refund, 0x0100_0000).");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.ReceiverIsBusiness) && string.IsNullOrEmpty(request.ReceiptRequest.cbCustomer?.ToString()))
        {
            request.ReceiptResponse.SetReceiptResponseError("A NIP receipt (paragon z NIP) requires the buyer's NIP in cbCustomer. Until 2026-12-31 such receipts up to 450 PLN act as simplified invoices.");
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>());
    }

    private static bool HasMixedSaleAndReturn(ReceiptRequest request)
    {
        var chargeItems = request.cbChargeItems ?? new List<ChargeItem>();
        var hasPositive = chargeItems.Any(x => x.Amount > 0);
        var hasNegative = chargeItems.Any(x => x.Amount < 0);
        return hasPositive && hasNegative;
    }
}
