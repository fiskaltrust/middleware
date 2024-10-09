using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.GR;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueGR;

public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    public Func<string, Task<string>> RegisterForSign(MiddlewareConfiguration middlewareConfiguration, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        var queueGR = new ftQueueGR();
        var signaturCreationUnitPT = new ftSignaturCreationUnitGR();
        var ptSSCD = new MyDataApiClient("", "");
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorGR(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorGR(ptSSCD, queueGR, signaturCreationUnitPT), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(), new ProtocolCommandProcessorGR(), queueGR.CashBoxIdentification);
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), storageProvider, signProcessorPT.ProcessAsync, queueGR.CashBoxIdentification, middlewareConfiguration);
        return async (string message) =>
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<ReceiptRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
            var response = await signProcessor.ProcessAsync(request);
            return System.Text.Json.JsonSerializer.Serialize(response);
        };
    }
}
