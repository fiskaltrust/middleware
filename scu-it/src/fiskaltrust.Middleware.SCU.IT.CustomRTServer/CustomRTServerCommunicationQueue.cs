using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;
#pragma warning disable

public class CustomRTServerCommunicationQueue
{
    private readonly Dictionary<string, List<CommercialDocument>> _receiptQueue = new Dictionary<string, List<CommercialDocument>>();
    private readonly CustomRTServerClient _client;
    private readonly ILogger<CustomRTServerCommunicationQueue> _logger;
    private readonly CustomRTServerConfiguration _customRTServerConfiguration;

    private List<string> _startedUploads = new List<string>();

    public CustomRTServerCommunicationQueue(CustomRTServerClient client, ILogger<CustomRTServerCommunicationQueue> logger, CustomRTServerConfiguration customRTServerConfiguration)
    {
        _client = client;
        _logger = logger;
        _customRTServerConfiguration = customRTServerConfiguration;
    }

    public async Task EnqueueDocument(string cashuuid, CommercialDocument commercialDocument)
    {
        if (_customRTServerConfiguration.SendReceiptsSync)
        {
            await _client.InsertFiscalDocumentAsync(cashuuid, commercialDocument);
        }
        else
        {
            if (!_receiptQueue.ContainsKey(cashuuid))
            {
                _receiptQueue[cashuuid] = new List<CommercialDocument>();
            }

            _receiptQueue[cashuuid].Add(commercialDocument);
            if (!_startedUploads.Contains(cashuuid))
            {
                Task.Run(() => ProcessReceiptsInBackground(cashuuid));
                _startedUploads.Add(cashuuid);
            }
        }
    }

    public async Task ProcessReceiptsInBackground(string cashuuid)
    {
        while (true)
        {
            if (!_receiptQueue.ContainsKey(cashuuid))
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }

            if (_receiptQueue[cashuuid].Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }

            // TODO we need to integrate more persistance
            foreach (var receipts in _receiptQueue[cashuuid].SplitList(10))
            {
                await _client.InsertFiscalDocumentArrayAsync(cashuuid, receipts);
            }
            _receiptQueue[cashuuid].Clear();
        }
    }

    public async Task ProcessAllReceipts(string cashuuid)
    {
        if (!_receiptQueue.ContainsKey(cashuuid))
        {
            return;
        }

        while (_receiptQueue[cashuuid].Count != 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }
}

public static class ListExtensions
{
    public static IEnumerable<List<T>> SplitList<T>(this List<T> locations, int nSize = 30)
    {
        for (int i = 0; i < locations.Count; i += nSize)
        {
            yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
        }
    }
}