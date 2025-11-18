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
        if (series.Numerator == 0)
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
        }
    }
}
