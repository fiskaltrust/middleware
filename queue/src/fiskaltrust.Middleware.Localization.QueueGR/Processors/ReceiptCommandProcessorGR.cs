using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class ReceiptCommandProcessorGR(
    IGRSSCD sscd,
    IQueueStorageProvider queueStorageProvider,
    AsyncLazy<IConfigurationRepository> configurationRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly IQueueStorageProvider _queueStorageProvider = queueStorageProvider;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => SubmitAsync(request);

    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => SubmitAsync(request);

    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => GRFallBackOperations.NotSupported(request, "PointOfSaleReceiptWithoutObligation");

    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => SubmitAsync(request);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request)
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(Models.Cases.ReceiptCaseFlags.HasTransportInformation))
        {
            return await SubmitAsync(request);
        }

        return await GRFallBackOperations.NotSupported(request, "DeliveryNote");
    }

    // TODO this should be 8.6
    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => GRFallBackOperations.NotSupported(request, "TableCheck0x0006Async");

    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request) => GRFallBackOperations.NotSupported(request, "ProForma0x0007Async");

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
