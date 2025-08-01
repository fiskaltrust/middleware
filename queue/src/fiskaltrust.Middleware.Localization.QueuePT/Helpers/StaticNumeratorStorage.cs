using System;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using Microsoft.Win32.SafeHandles;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public class StaticNumeratorStorage
{
    public static NumberSeries InvoiceSeries { get; set; } = new NumberSeries
    {
        TypeCode = "FT",
        ATCUD = "G4QZMNKE",
        Identifier = "FT ft2024"
    };

    public static NumberSeries SimplifiedInvoiceSeries { get; set; } = new NumberSeries
    {
        TypeCode = "FS",
        ATCUD = "XBPRP1MS",
        Identifier = "FS ft2024"
    };

    public static NumberSeries CreditNoteSeries { get; set; }= new NumberSeries
    {
        TypeCode = "NC",
        ATCUD = "QRFQ68NC",
        Identifier = "NC ft2024"
    };

    public static NumberSeries ProFormaSeries { get; set; } = new NumberSeries
    {
        TypeCode = "PF",
        ATCUD = "GGRS68NF",
        Identifier = "PF ft2024"
    };

    public static NumberSeries PaymentSeries { get; set; } = new NumberSeries
    {
        TypeCode = "RG",
        ATCUD = "FSSJ34SE",
        Identifier = "RG ft2024"
    };

    public static async Task LoadStorageNumbers(IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        var queueItems = (await middlewareQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow).ToList();
        ReloadSeries(SimplifiedInvoiceSeries, queueItems);
        ReloadSeries(CreditNoteSeries, queueItems);
        ReloadSeries(InvoiceSeries, queueItems);
        ReloadSeries(ProFormaSeries, queueItems);
        ReloadSeries(PaymentSeries, queueItems);
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
                    var lastSignature = lastReceiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == SignatureTypePT.Hash.As<SignatureType>());
                    if (lastSignature != null)
                    {
                        series.LastHash = lastSignature.Data;
                    }
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