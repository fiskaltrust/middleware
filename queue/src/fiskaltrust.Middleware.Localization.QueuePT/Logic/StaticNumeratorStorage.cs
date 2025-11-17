using System;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using Microsoft.Win32.SafeHandles;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public class StaticNumeratorStorage
{
    //public NumberSeries InvoiceSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "FT",
    //    ATCUD = "AAJFJGVC33",
    //    Series = "ft2025019d"
    //};

    //public NumberSeries SimplifiedInvoiceSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "FS",
    //    ATCUD = "AAJFJ4VC3W",
    //    Series = "ft2025019d"
    //};

    //public NumberSeries CreditNoteSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "NC",
    //    ATCUD = "AAJFJ3VC34",
    //    Series = "ft2025019d"
    //};

    //public NumberSeries HandWrittenFSSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "FS",
    //    ATCUD = "AAJFJBKFZR",
    //    Series = "ft2025771b"
    //};

    //public NumberSeries ProFormaSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "PF",
    //    ATCUD = "AAJFJ9VC37",
    //    Series = "ft2025019d"
    //};

    //public NumberSeries PaymentSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "RG",
    //    ATCUD = "AAJFJMVC3G",
    //    Series = "ft2025019d"
    //};

    //public NumberSeries BudgetSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "OR",
    //    ATCUD = "AAJFJKVC3P",
    //    Series = "ft2025eb51"
    //};

    //public NumberSeries TableChecqueSeries { get; set; } = new NumberSeries
    //{
    //    TypeCode = "CM",
    //    ATCUD = "AAJFJ2VC3R",
    //    Series = "ft2025eb51"
    //};


    public static async Task<NumeratorStorage> GetStaticNumeratorStorageAsync(ftQueuePT queuePT, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        await LoadStorageNumbers(queuePT.NumeratorStorage, middlewareQueueItemRepository);
        return queuePT.NumeratorStorage;
    }

    public static async Task LoadStorageNumbers(NumeratorStorage numeratorStorage, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        var queueItems = (await middlewareQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow).ToList();
        ReloadSeries(numeratorStorage.SimplifiedInvoiceSeries, queueItems);
        ReloadSeries(numeratorStorage.CreditNoteSeries, queueItems);
        ReloadSeries(numeratorStorage.InvoiceSeries, queueItems);
        ReloadSeries(numeratorStorage.ProFormaSeries, queueItems);
        ReloadSeries(numeratorStorage.PaymentSeries, queueItems);
        ReloadSeries(numeratorStorage.BudgetSeries, queueItems);
        ReloadSeries(numeratorStorage.TableChecqueSeries, queueItems);
        ReloadSeries(numeratorStorage.HandWrittenFSSeries, queueItems);
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
                    var lastSignature = lastReceiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.Hash));
                    if (lastSignature != null)
                    {
                        series.LastHash = lastSignature.Data;
                        series.LastCbReceiptMoment = lastReceiptResponse.ftReceiptMoment;
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
