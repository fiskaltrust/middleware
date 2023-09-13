using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class CustomRTServerCommunicationQueue
{
    private readonly List<CommercialDocument> _receiptQueue = new List<CommercialDocument>();
    private readonly CustomRTServerClient _client;
    private readonly ILogger<CustomRTServerCommunicationQueue> _logger;

    public CustomRTServerCommunicationQueue(CustomRTServerClient client, ILogger<CustomRTServerCommunicationQueue> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnqueueDocument(CommercialDocument commercialDocument)
    {
        _receiptQueue.Add(commercialDocument);
        await Task.Run(() => _client.InsertFiscalDocumentAsync(commercialDocument)).ContinueWith(x =>
          {
              if (x.IsFaulted)
              {
                  _logger.LogError(x.Exception, "Failed to insert fiscal document");
              }
              else
              {
                  _logger.LogInformation("Transmitted commercial document with sha {shametadata}.", commercialDocument.qrData.shaMetadata);
              }
          });
    }

    public async Task ProcessAllReceipts()
    {
        await Task.CompletedTask;
    }
}