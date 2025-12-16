using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2;
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.ifPOS.v2.be;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class InvoiceCommandProcessorBE(IBESSCD sscd, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : IInvoiceCommandProcessor
{
#pragma warning disable
    private readonly IBESSCD _sscd = sscd;
#pragma warning restore

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);
}