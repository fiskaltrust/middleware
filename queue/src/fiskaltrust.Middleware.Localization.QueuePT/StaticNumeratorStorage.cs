using System;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using Microsoft.Win32.SafeHandles;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class StaticNumeratorStorage
{
    public static NumberSeries InvoiceSeries { get; set; } = new NumberSeries
    {
        TypeCode = "FT",
        ATCUD = "G4QZMNK",
        Identifier = "FT ft2024"
    };

    public static NumberSeries SimplifiedInvoiceSeries { get; set; } = new NumberSeries
    {
        TypeCode = "FS",
        ATCUD = "XBPRP1M",
        Identifier = "FS ft2024"
    };

    public static NumberSeries CreditNoteSeries { get; set; }= new NumberSeries
    {
        TypeCode = "NC",
        ATCUD = "QRFQ68N",
        Identifier = "NC ft2024"
    };

    public static NumberSeries ProFormaSeries { get; set; } = new NumberSeries
    {
        TypeCode = "PF",
        ATCUD = "GGRS68N",
        Identifier = "PF ft2024"
    };

    public static async Task LoadStorageNumbers(IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        var queueItems = (await middlewareQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow).ToList();
        ReloadSeries(StaticNumeratorStorage.SimplifiedInvoiceSeries, queueItems);
        ReloadSeries(StaticNumeratorStorage.CreditNoteSeries, queueItems);
        ReloadSeries(StaticNumeratorStorage.InvoiceSeries, queueItems);
    }

    private static void ReloadSeries(NumberSeries series, List<ftQueueItem> queueItems)
    {
        if (series.Numerator == null)
        {
            foreach (var queueItem in queueItems)
            {
                if (string.IsNullOrEmpty(queueItem.response))
                {
                    continue;
                }

                var lastReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (lastReceiptResponse == null)
                {
                    continue;
                }

                if (!lastReceiptResponse.ftState.HasFlag(State.Success))
                {
                    continue;
                }


                if (lastReceiptResponse.ftReceiptIdentification.StartsWith(series.Identifier))
                {
                    series.Numerator = int.Parse(lastReceiptResponse.ftReceiptIdentification.Split("/")[1]);
                    break;
                }
            }
            if (series.Numerator == null)
            {
                series.Numerator = 0;
            }
        }
    }
}


public class NumberSeries
{
    public required string TypeCode { get; set; }
    public required string ATCUD { get; set; }
    public required string Identifier { get; set; }
    public long? Numerator { get; set; }
    public string? LastHash { get; set; }
}