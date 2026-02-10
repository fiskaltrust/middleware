using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public class StaticNumeratorStorage
{
    public static async Task<NumberSeries> GetNumberSeriesAsync(ReceiptRequest receiptRequest, ftQueuePT queuePT, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        var numeratorStorage = await GetStaticNumeratorStorageAsync(queuePT, middlewareQueueItemRepository);
        var isRefund = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) || receiptRequest.IsPartialRefundReceipt();
        var isHandwritten = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten);

        if (isRefund)
        {
            return numeratorStorage.CreditNoteSeries;
        }
        else if (isHandwritten)
        {
            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceB2C0x1001) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceB2B0x1002) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceB2G0x1003) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceUnknown0x1000))
            {
                if(numeratorStorage.HandWrittenFTSeries == null)
                {
                    throw new NotSupportedException("HandWritten series is not correctly configured.");
                }

                return numeratorStorage.HandWrittenFTSeries;
            }
            else
            {
                throw new NotSupportedException("HandWritten receipt case is not supported for this type of receipt.");
            }
        }
        else
        {
            if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.UnknownReceipt0x0000))
            {
                return numeratorStorage.SimplifiedInvoiceSeries;
            }
            else if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceUnknown0x1000) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceB2C0x1001) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceB2B0x1002) || receiptRequest.ftReceiptCase.IsCase(ReceiptCase.InvoiceB2G0x1003))
            {
                return numeratorStorage.InvoiceSeries;
            }
            else if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002))
            {
                return numeratorStorage.PaymentSeries;
            }
            else if (receiptRequest.ftReceiptCase.IsCase((ReceiptCase) 0x0006))
            {
                return numeratorStorage.TableChecqueSeries;
            }
            else if (receiptRequest.ftReceiptCase.IsCase((ReceiptCase) 0x0007))
            {
                if ((receiptRequest.ftReceiptCase & (ReceiptCase) 0x0000_0002_0000_0000) == (ReceiptCase) 0x0000_0002_0000_0000)
                {
                    return numeratorStorage.BudgetSeries;
                }
                else
                {
                    return numeratorStorage.ProFormaSeries;
                }
            }
            else
            {
                throw new NotSupportedException("Receipt case is not supported for this type of receipt.");
            }
        }
    }

    public static async Task<NumeratorStorage> GetStaticNumeratorStorageAsync(ftQueuePT queuePT, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        await LoadStorageNumbers(queuePT.NumeratorStorage, middlewareQueueItemRepository);
        return queuePT.NumeratorStorage;
    }

    public static async Task LoadStorageNumbers(NumeratorStorage numeratorStorage, IMiddlewareQueueItemRepository middlewareQueueItemRepository)
    {
        // this loop over all queueitems can't stay.
        var queueItems = (await middlewareQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow).ToList();
        ReloadSeries(numeratorStorage.SimplifiedInvoiceSeries, queueItems);
        ReloadSeries(numeratorStorage.CreditNoteSeries, queueItems);
        ReloadSeries(numeratorStorage.InvoiceSeries, queueItems);
        ReloadSeries(numeratorStorage.ProFormaSeries, queueItems);
        ReloadSeries(numeratorStorage.PaymentSeries, queueItems);
        ReloadSeries(numeratorStorage.BudgetSeries, queueItems);
        ReloadSeries(numeratorStorage.TableChecqueSeries, queueItems);
        if (numeratorStorage.HandWrittenFTSeries != null)
        {
            ReloadSeries(numeratorStorage.HandWrittenFTSeries, queueItems);
        }
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

                var lastReceiptRequest = JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                var lastReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                if (lastReceiptResponse == null || lastReceiptRequest == null)
                {
                    continue;
                }

                if (!lastReceiptResponse.ftState.HasFlag(State.Success))
                {
                    continue;
                }

                if (lastReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
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
