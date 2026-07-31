using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class ProtocolCommandProcessorGR(
    IGRSSCD sscd,
    IQueueStorageProvider queueStorageProvider,
    AsyncLazy<IConfigurationRepository> configurationRepository) : IProtocolCommandProcessor
{
    private readonly IGRSSCD _sscd = sscd;
    private readonly IQueueStorageProvider _queueStorageProvider = queueStorageProvider;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => GRFallBackOperations.NotSupported(request, "InternalUsageMaterialConsumption");

    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => SubmitAsync(request);

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request)
    {
        var hasLocalPayItemFlag = request.ReceiptRequest.cbPayItems.Any(p => ((long) p.ftPayItemCase & 0x0000_0001_0000_0000) != 0);
        if (!hasLocalPayItemFlag || request.ReceiptRequest.cbPreviousReceiptReference == null)
        {
            return await GRFallBackOperations.NoOp(request);
        }

        return await SubmitAsync(request);
    }

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    // Everything that calls the SCU runs through the counter reservation. Whether an
    // invoice was actually filed is decided by the mark gate inside the reservation:
    // orders are submitted via SendInvoices and consume an aa, while Pay0x3005 is
    // transmitted via the payment-methods endpoint, produces no invoiceMark and
    // therefore never consumes one.
    private Task<ProcessCommandResponse> SubmitAsync(ProcessCommandRequest request) =>
        InvoiceCounterReservation.InvokeWithCounterAsync(
            request,
            _configurationRepository,
            async () =>
            {
                var receiptReferences = await _queueStorageProvider.GetReceiptReferencesIfNecessaryAsync(request);
                return await _sscd.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request.ReceiptRequest,
                    ReceiptResponse = request.ReceiptResponse,
                }, receiptReferences);
            });
}
