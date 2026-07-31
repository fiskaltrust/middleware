using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class InvoiceCommandProcessorGR(
    IGRSSCD sscd,
    IQueueStorageProvider queueStorageProvider,
    AsyncLazy<IConfigurationRepository> configurationRepository) : IInvoiceCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly IQueueStorageProvider _queueStorageProvider = queueStorageProvider;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;
#pragma warning restore

    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) =>
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

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => InvoiceUnknown0x1000Async(request);

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => InvoiceUnknown0x1000Async(request);

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => InvoiceUnknown0x1000Async(request);
}
