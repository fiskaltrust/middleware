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
        ATCUD = "AAJFJGVC33",
        Identifier = "FT ft2025019d",
    };

    public static NumberSeries SimplifiedInvoiceSeries { get; set; } = new NumberSeries
    {
        TypeCode = "FS",
        ATCUD = "AAJFJ4VC3W",
        Identifier = "FSft2025019d",
    };

    public static NumberSeries CreditNoteSeries { get; set; }= new NumberSeries
    {
        TypeCode = "NC",
        ATCUD = "AAJFJ3VC34",
        Identifier = "NC ft2025019d",
    };

    public static NumberSeries HandWrittenFSSeries { get; set; } = new NumberSeries
    {
        TypeCode = "FSM",
        ATCUD = "AAJFJ4VC3W",
        Identifier = "FSM ft2025019d",
    };

    public static NumberSeries ProFormaSeries { get; set; } = new NumberSeries
    {
        TypeCode = "PF",
        ATCUD = "AAJFJ9VC37",
        Identifier = "PF ft2025019d",
    };

    public static NumberSeries PaymentSeries { get; set; } = new NumberSeries
    {
        TypeCode = "RG",
        ATCUD = "AAJFJMVC3G",
        Identifier = "RG ft2025019d",
    };

    public static NumberSeries BudgetSeries { get; set; } = new NumberSeries
    {
        TypeCode = "OR",
        ATCUD = "AAJFJKVC3P",
        Identifier = "OR ft2025eb51",
    };

    public static NumberSeries TableChecqueSeries { get; set; } = new NumberSeries
    {
        TypeCode = "CM",
        ATCUD = "AAJFJ2VC3R",
        Identifier = "CM ft2025eb51",
    };


    public static async Task LoadStorageNumbers(IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        var queueItems = (await middlewareQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow).ToList();
        ReloadSeries(SimplifiedInvoiceSeries, queueItems);
        ReloadSeries(CreditNoteSeries, queueItems);
        ReloadSeries(InvoiceSeries, queueItems);
        ReloadSeries(ProFormaSeries, queueItems);
        ReloadSeries(PaymentSeries, queueItems);
        ReloadSeries(BudgetSeries, queueItems);
        ReloadSeries(TableChecqueSeries, queueItems);
        ReloadSeries(HandWrittenFSSeries, queueItems);
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


                if (lastReceiptResponse.ftReceiptIdentification.Split("#").Last().StartsWith(series.Identifier))
                {
                    series.Numerator = int.Parse(lastReceiptResponse.ftReceiptIdentification.Split("#").Last().Split("/")[1]);
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