using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.Abstraction.Validation;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Newtonsoft.Json;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public sealed class EpsonRTPrinterSCU : LegacySCU
{
    /// <summary>
    /// Reported when the connection to the printer broke and we cannot tell whether the document was
    /// printed. The caller must resolve it by looking at the printer, not by sending the receipt again.
    /// </summary>
    public const string UnknownDocumentStateError = "epson-printer-network-error-unknown-document-state";

    private readonly ILogger<EpsonRTPrinterSCU> _logger;
    private readonly IEpsonFpMateClient _httpClient;
    private readonly EpsonRTPrinterSCUConfiguration _configuration;
    private readonly ErrorInfoFactory _errorCodeFactory = new();
    private string? _serialnr;
    private readonly LastDocSlot _lastDoc;

    /// <param name="lastDoc">
    /// Baseline of the RT document counter, used by the network-error recovery. Optional: when nothing is
    /// passed the SCU keeps a slot of its own, which is exactly the lifetime the baseline had when it was a
    /// plain field — so on-prem hosts, which never pass it, behave as before. Hosts that rebuild the SCU per
    /// request (CloudRTDevice) pass a slot that outlives the request, one per cashbox.
    /// </param>
    public EpsonRTPrinterSCU(ILogger<EpsonRTPrinterSCU> logger, EpsonRTPrinterSCUConfiguration configuration, IEpsonFpMateClient epsonCloudHttpClient, LastDocSlot? lastDoc = null)
    {
        _logger = logger;
        _httpClient = epsonCloudHttpClient;
        _configuration = configuration;
        _lastDoc = lastDoc ?? new LastDocSlot();
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public override async Task<RTInfo> GetRTInfoAsync()
    {
        var result = await QueryPrinterStatusAsync();
        _logger.LogInformation(JsonConvert.SerializeObject(result));
        _serialnr = "";
        if (string.IsNullOrEmpty(_serialnr) && result?.Printerstatus?.RtType != null)
        {
            _serialnr = await GetSerialNumberAsync(result.Printerstatus.RtType).ConfigureAwait(false);
        }
        return new RTInfo
        {
            SerialNumber = _serialnr,
            InfoData = JsonConvert.SerializeObject(new DeviceInfo
            {
                DailyOpen = result?.Printerstatus?.DailyOpen == "1",
                DeviceStatus = Helpers.ParseStatus(result?.Printerstatus?.MfStatus),
                ExpireDeviceCertificateDate = result?.Printerstatus?.ExpiryCD,
                ExpireTACommunicationCertificateDate = result?.Printerstatus?.ExpiryCA,
                SerialNumber = _serialnr
            })
        };
    }

    private async Task<StatusResponse?> QueryPrinterStatusAsync()
    {
        var queryPrinterStatus = new QueryPrinterStatusCommand { QueryPrinterStatus = new QueryPrinterStatus { StatusType = 1 } };
        var response = await _httpClient.SendCommandAsync(SoapSerializer.Serialize(queryPrinterStatus));
        using var responseContent = await response.Content.ReadAsStreamAsync();
        return SoapSerializer.DeserializeToSoapEnvelope<StatusResponse>(responseContent);
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            var receiptCase = request.ReceiptRequest.GetReceiptCase();

            // Reject a malformed codice fiscale / partita IVA before anything is sent to the printer.
            if (request.ReceiptRequest.CarriesCustomerTaxIds()
                && !request.ReceiptRequest.TryValidateCustomerTaxIds(out var customerTaxIdError))
            {
                _logger.LogWarning("({receiptreference}) Rejected: {error}", request.ReceiptRequest.cbReceiptReference, customerTaxIdError);
                request.ReceiptResponse.SetReceiptResponseErrored(CustomerTaxIdValidation.CustomerTaxIdErrorCaption, customerTaxIdError!);
                return Helpers.CreateResponse(request.ReceiptResponse);
            }

            if (request.ReceiptRequest.IsInitialOperationReceipt())
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateInitialOperationSignatures().ToList());
            }

            if (request.ReceiptRequest.IsOutOfOperationReceipt())
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateOutOfOperationSignatures().ToList());
            }

            if (request.ReceiptRequest.IsZeroReceipt())
            {
                return await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse);
            }

            if (request.ReceiptRequest.IsVoid())
            {
                return await ProcessVoidReceipt(request);
            }

            if (request.ReceiptRequest.IsRefund())
            {
                return await ProcessRefundReceipt(request);
            }

            if (request.ReceiptRequest.IsDailyClosing())
            {
                return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }

            if (request.ReceiptRequest.IsMonthlyClosing())
            {
                return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }

            if (request.ReceiptRequest.IsYearlyClosing())
            {
                return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }

            if (request.ReceiptRequest.IsReprint())
            {
                return await ProcessPerformReprint(request);
            }

            if (receiptCase == (long) ITReceiptCases.ProtocolUnspecified0x3000 && ((request.ReceiptRequest.ftReceiptCase & 0x0000_0002_0000_0000) != 0))
            {
                return await ProcessUnspecifiedProtocolReceipt(request);
            }

            if (receiptCase == (long) ITReceiptCases.DeliveryNote0x0005)
            {
                return Helpers.CreateResponse(await PerformProtocolReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            }

            switch (receiptCase)
            {
                case (long) ITReceiptCases.UnknownReceipt0x0000:
                case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
                    return Helpers.CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            }
            request.ReceiptResponse.SetReceiptResponseErrored($"The given receiptcase 0x{receiptCase.ToString("X")} is not supported by Epson RT Printer.");
            return Helpers.CreateResponse(request.ReceiptResponse);
        }
        catch (Exception ex)
        {
            request.ReceiptResponse.SetReceiptResponseErrored("epson-printer-generic-error", ex.ToString());
            return Helpers.CreateResponse(request.ReceiptResponse);
        }
    }

    private async Task<FiscalReceiptResponse> SetReceiptResponse(PrinterReceiptResponse? result)
    {
        var fiscalReceiptResponse = new FiscalReceiptResponse
        {
            Success = result?.Success ?? false
        };
        if (result?.Success == false)
        {
            fiscalReceiptResponse.SSCDErrorInfo = GetErrorInfo(result.Code, result.Status, result?.Receipt?.PrinterStatus);
            await ResetPrinter();
        }
        else
        {
            fiscalReceiptResponse.ReceiptNumber = result?.Receipt?.FiscalReceiptNumber != null ? long.Parse(result.Receipt.FiscalReceiptNumber) : 0;
            fiscalReceiptResponse.ZRepNumber = result?.Receipt?.ZRepNumber != null ? long.Parse(result.Receipt.ZRepNumber) : 0;
            if (result?.Receipt?.FiscalReceiptDate != null && result?.Receipt?.FiscalReceiptTime != null)
            {
                fiscalReceiptResponse.ReceiptDateTime = DateTime.ParseExact(result.Receipt.FiscalReceiptDate, "d/M/yyyy", CultureInfo.InvariantCulture);
                var time = TimeSpan.Parse(result.Receipt.FiscalReceiptTime);
                fiscalReceiptResponse.ReceiptDateTime = fiscalReceiptResponse.ReceiptDateTime + time;
            }
            else
            {
                fiscalReceiptResponse.ReceiptDateTime = DateTime.Now; // ???????
            }
        }
        return fiscalReceiptResponse;
    }

    private async Task<FiscalReceiptResponse> SetReceiptResponse(PrinterResponse? result)
    {
        var fiscalReceiptResponse = new FiscalReceiptResponse
        {
            Success = result?.Success ?? false
        };
        if (result?.Success == false)
        {
            fiscalReceiptResponse.SSCDErrorInfo = GetErrorInfo(result.Code, result.Status, result?.Receipt?.PrinterStatus);
            await ResetPrinter();
        }
        else
        {
            fiscalReceiptResponse.ReceiptNumber = result?.Receipt?.FiscalReceiptNumber != null ? long.Parse(result.Receipt.FiscalReceiptNumber) : 0;
            fiscalReceiptResponse.ZRepNumber = result?.Receipt?.ZRepNumber != null ? long.Parse(result.Receipt.ZRepNumber) : 0;
            if (result?.Receipt?.FiscalReceiptDate != null && result?.Receipt?.FiscalReceiptTime != null)
            {
                fiscalReceiptResponse.ReceiptDateTime = DateTime.ParseExact(result.Receipt.FiscalReceiptDate, "d/M/yyyy", CultureInfo.InvariantCulture);
                var time = TimeSpan.Parse(result.Receipt.FiscalReceiptTime);
                fiscalReceiptResponse.ReceiptDateTime = fiscalReceiptResponse.ReceiptDateTime + time;
            }
            else
            {
                fiscalReceiptResponse.ReceiptDateTime = DateTime.Now; // ???????
            }
        }
        return fiscalReceiptResponse;
    }

    private static string GetLotteryCode(ReceiptRequest receiptRequest)
        => receiptRequest.GetLotteryData()?.servizi_lotteriadegliscontrini_gov_it?.codicelotteria ?? "";

    /// <summary>
    /// Customer tax identifier reported in the RTCustomerID signature. Uses the same selection rule as
    /// EpsonCommandFactory, so the signature always carries what was actually sent to the printer: on the
    /// document types signed here (POSRECEIPT/REFUND/VOID) the request level validation has already run,
    /// so a non empty value is a valid codice fiscale or partita IVA.
    /// </summary>
    private static string GetCustomerTaxId(ReceiptRequest receiptRequest)
    {
        var customer = receiptRequest.GetCustomer();
        return ItalyValidationHelpers.SelectCustomerTaxId(customer?.CustomerId, customer?.CustomerVATId);
    }

    public async Task<ReceiptResponse> PerformProtocolReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        string? data = null;
        try
        {
            await EnsureLastDocBaselineAsync(receiptRequest.cbReceiptReference);
            var content = EpsonCommandFactory.CreateInvoiceRequestContent(_configuration, receiptRequest);
            var customerData = receiptRequest.GetCustomer();
            if (customerData != null)
            {
                if (content.PrintRecMessageType3 == null)
                {
                    content.PrintRecMessageType3 = new List<PrintRecMessage>();
                }
                content.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 2,
                    Index = "1",
                    Message = customerData.CustomerName ?? ""
                });
                content.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 2,
                    Index = "2",
                    Message = customerData.CustomerStreet ?? ""
                });
                content.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 2,
                    Index = "3",
                    Message = string.Format("{0} {1} {2}", customerData.CustomerCountry ?? "", customerData.CustomerZip ?? "", customerData.CustomerCity ?? "")
                });
            }

            data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }

            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                receiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return receiptResponse;
            }
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber ?? _serialnr ?? "",
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = GetLotteryCode(receiptRequest),
                RTCustomerID = GetCustomerTaxId(receiptRequest),
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            _lastDoc.Value = new DocPosition(fiscalReceiptResponse.ZRepNumber, fiscalReceiptResponse.ReceiptNumber);

            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                receiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }

            return receiptResponse;
        }
        catch (Exception e) when ((e is TaskCanceledException || e is HttpRequestException) && data != null)
        {
            _logger.LogWarning("({receiptreference}) Network error — checking if the printer has already printed...", receiptRequest.cbReceiptReference);
            return await TryRecoverFromNetworkErrorAsync(receiptRequest, receiptResponse, data);
        }
        catch (Exception e)
        {
            var response = Helpers.ExceptionInfo(e);
            receiptResponse.SetReceiptResponseErrored(response.SSCDErrorInfo?.Info ?? "");
            return receiptResponse;
        }
    }

    public async Task<ReceiptResponse> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        string? data = null;
        try
        {
            await EnsureLastDocBaselineAsync(receiptRequest.cbReceiptReference);
            var content = EpsonCommandFactory.CreateInvoiceRequestContent(_configuration, receiptRequest);
            data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, data);
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                _logger.LogError("Error while processing classic receipt: {error}", fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "NOERROR");
                receiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return receiptResponse;
            }
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber ?? "",
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = GetLotteryCode(receiptRequest),
                RTCustomerID = GetCustomerTaxId(receiptRequest),
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            _lastDoc.Value = new DocPosition(fiscalReceiptResponse.ZRepNumber, fiscalReceiptResponse.ReceiptNumber);
            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                receiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }
            return receiptResponse;
        }
        catch (Exception e) when ((e is TaskCanceledException || e is HttpRequestException) && data != null)
        {
            _logger.LogWarning("({receiptreference}) Network error — checking if the printer has already printed...", receiptRequest.cbReceiptReference);
            return await TryRecoverFromNetworkErrorAsync(receiptRequest, receiptResponse, data);
        }
        catch (Exception e)
        {
            var response = Helpers.ExceptionInfo(e);
            _logger.LogError(e, "Error while processing classic receipt: {error}", response.SSCDErrorInfo?.Info);
            receiptResponse.SetReceiptResponseErrored(response.SSCDErrorInfo?.Info ?? "");
            return receiptResponse;
        }
    }

    /// <summary>
    /// Reads the current document counter from the printer and stores it as the recovery baseline, unless a
    /// baseline is already known.
    /// <para>
    /// This has to run <em>before</em> the receipt is sent: once the command has failed, the last emitted
    /// document already includes the receipt the printer may have printed, so it no longer tells the two
    /// cases apart. A failure here is not fatal — the baseline stays unknown and
    /// <see cref="TryRecoverFromNetworkErrorAsync"/> then refuses to reprint.
    /// </para>
    /// </summary>
    private async Task EnsureLastDocBaselineAsync(string receiptReference)
    {
        if (_lastDoc.Value != null)
        {
            return;
        }

        try
        {
            var current = await ReadLastEmittedDocStatusAsync(receiptReference);
            // Only a fiscal document is a usable baseline: the progressive number of a non-fiscal document
            // comes from a different counter, and comparing the two could report progress that never happened.
            if (current != null && current.IsFiscalDocument)
            {
                _lastDoc.Value = new DocPosition(current.ZNumber, current.DocNumber);
                _logger.LogDebug("({receiptreference}) Recovery baseline initialised from printer: Z#{z} Doc#{doc}.",
                    receiptReference, current.ZNumber, current.DocNumber);
            }
            else
            {
                _logger.LogWarning("({receiptreference}) Could not establish a recovery baseline (last emitted document is {state}). A network error on this receipt will be reported as an error instead of retried.",
                    receiptReference, current == null ? "unavailable" : "not fiscal");
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "({receiptreference}) Could not establish a recovery baseline. A network error on this receipt will be reported as an error instead of retried.", receiptReference);
        }
    }

    /// <summary>
    /// Drops the recovery baseline. Every operation that can move the printer counters without the resulting
    /// Z#/Doc# being recorded here has to call this, otherwise the stale baseline would make the next failing
    /// receipt look "advanced" and recover somebody else's document.
    /// </summary>
    private void InvalidateLastDocBaseline(string reason)
    {
        if (_lastDoc.Value != null)
        {
            _logger.LogDebug("Recovery baseline dropped ({reason}); it will be read from the printer again.", reason);
        }
        _lastDoc.Value = null;
    }

    private async Task<ReceiptResponse> TryRecoverFromNetworkErrorAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string xmlData)
    {
        _logger.LogInformation("({receiptreference}) Querying last emitted document from printer...", receiptRequest.cbReceiptReference);
        var docBeforeRetry = await ReadLastEmittedDocStatusAsync(receiptRequest.cbReceiptReference);
        var baseline = _lastDoc.Value;
        _logger.LogDebug("({receiptreference}) Last emitted doc: Z#{z} Doc#{doc} amount={amount}cents, baseline: Z#{bz} Doc#{bd}",
            receiptRequest.cbReceiptReference, docBeforeRetry?.ZNumber, docBeforeRetry?.DocNumber, docBeforeRetry?.TotalDocAmountCents, baseline?.ZNumber, baseline?.DocNumber);

        if (baseline == null || !IsComparable(docBeforeRetry))
        {
            // Either end of the comparison is missing, so the printer state is unknown: the document may or
            // may not have been printed. Retrying would risk a second fiscal document for the same sale, so
            // report the error instead. Note that a failed status read is *not* the same as "no progress" —
            // conflating the two is what would turn a lost answer into a duplicate.
            _logger.LogError("({receiptreference}) Printer state unknown ({missing}) — refusing to reprint.",
                receiptRequest.cbReceiptReference, baseline == null ? "no baseline" : "last emitted document unreadable");
            receiptResponse.SetReceiptResponseErrored(UnknownDocumentStateError);
            return receiptResponse;
        }

        if (IsDocAdvanced(docBeforeRetry, baseline))
        {
            _logger.LogInformation("({receiptreference}) Document found: Z#{zNum} Doc#{docNum} — printer already printed, skipping retry.",
                receiptRequest.cbReceiptReference, docBeforeRetry!.ZNumber, docBeforeRetry.DocNumber);
            return ApplyRecoveredDoc(receiptResponse, docBeforeRetry, GetLotteryCode(receiptRequest));
        }

        return await RetryReceiptWithRecoveryAsync(receiptRequest, receiptResponse, xmlData, baseline);
    }

    /// <param name="baseline">
    /// Document counter as it was before the receipt was sent. It is required, not optional: reaching this
    /// method means we know the printer had not printed yet, which is the only state in which sending the
    /// receipt again cannot produce a duplicate fiscal document.
    /// </param>
    private async Task<ReceiptResponse> RetryReceiptWithRecoveryAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string xmlData, DocPosition baseline)
    {
        for (var attempt = 0; attempt < _configuration.MaxNetworkRetries; attempt++)
        {
            await Task.Delay(1000);
            try
            {
                _logger.LogWarning("({receiptreference}) Document not found — retrying receipt (attempt {attempt}/{max})...", receiptRequest.cbReceiptReference, attempt + 1, _configuration.MaxNetworkRetries);
                var retryResponse = await _httpClient.SendCommandAsync(xmlData);
                using var retryContent = await retryResponse.Content.ReadAsStreamAsync();
                var retryResult = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(retryContent);
                var retryFiscalResponse = await SetReceiptResponse(retryResult);
                if (retryFiscalResponse.Success)
                {
                    _logger.LogInformation("({receiptreference}) Retry succeeded: Z#{zNum} Doc#{docNum}.", receiptRequest.cbReceiptReference, retryFiscalResponse.ZRepNumber, retryFiscalResponse.ReceiptNumber);
                    _lastDoc.Value = new DocPosition(retryFiscalResponse.ZRepNumber, retryFiscalResponse.ReceiptNumber);
                    receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(new POSReceiptSignatureData
                    {
                        RTSerialNumber = retryResult?.Receipt?.SerialNumber ?? "",
                        RTZNumber = retryFiscalResponse.ZRepNumber,
                        RTDocNumber = retryFiscalResponse.ReceiptNumber,
                        RTDocMoment = retryFiscalResponse.ReceiptDateTime,
                        RTDocType = "POSRECEIPT",
                        RTCodiceLotteria = GetLotteryCode(receiptRequest),
                        RTCustomerID = "",
                    }).ToArray();
                    return receiptResponse;
                }

                _logger.LogError("({receiptreference}) Retry attempt {attempt}/{max} failed with printer error: {error}", receiptRequest.cbReceiptReference, attempt + 1, _configuration.MaxNetworkRetries, retryFiscalResponse.SSCDErrorInfo?.Info);
                receiptResponse.SetReceiptResponseErrored(retryFiscalResponse.SSCDErrorInfo?.Info ?? "");
                return receiptResponse;
            }
            catch (Exception e) when (e is TaskCanceledException || e is HttpRequestException)
            {
                _logger.LogWarning("({receiptreference}) Network error on retry attempt {attempt}/{max} — checking if printer has printed...", receiptRequest.cbReceiptReference, attempt + 1, _configuration.MaxNetworkRetries);
                var lastDoc = await ReadLastEmittedDocStatusAsync(receiptRequest.cbReceiptReference);
                _logger.LogDebug("({receiptreference}) Current: Z#{z} Doc#{doc}, baseline: Z#{bz} Doc#{bd}", receiptRequest.cbReceiptReference, lastDoc?.ZNumber, lastDoc?.DocNumber, baseline.ZNumber, baseline.DocNumber);

                if (IsDocAdvanced(lastDoc, baseline))
                {
                    _logger.LogInformation("({receiptreference}) Document found: Z#{zNum} Doc#{docNum} — printer already printed, skipping retry.", receiptRequest.cbReceiptReference, lastDoc!.ZNumber, lastDoc.DocNumber);
                    return ApplyRecoveredDoc(receiptResponse, lastDoc, GetLotteryCode(receiptRequest));
                }

                if (!IsComparable(lastDoc))
                {
                    // The attempt we just made may have printed and we can no longer check. Another attempt
                    // would be a coin flip on a fiscal document, so stop here.
                    _logger.LogError("({receiptreference}) Printer state unknown after attempt {attempt}/{max} — refusing to send the receipt again.", receiptRequest.cbReceiptReference, attempt + 1, _configuration.MaxNetworkRetries);
                    receiptResponse.SetReceiptResponseErrored(UnknownDocumentStateError);
                    return receiptResponse;
                }
            }
            catch (Exception queryEx)
            {
                _logger.LogError(queryEx, "({receiptreference}) Unexpected error on attempt {attempt}/{max}.", receiptRequest.cbReceiptReference, attempt + 1, _configuration.MaxNetworkRetries);
                break;
            }
        }

        _logger.LogError("({receiptreference}) All recovery attempts failed — unable to determine printer state.", receiptRequest.cbReceiptReference);
        receiptResponse.SetReceiptResponseErrored("epson-printer-network-error");
        return receiptResponse;
    }

    /// <summary>
    /// Whether the printer actually told us where it stands. The baseline is only ever taken from a fiscal
    /// document, so anything else cannot be compared against it — and "I could not read the counter" must
    /// never be mistaken for "the counter did not move".
    /// </summary>
    private static bool IsComparable(LastEmittedDocStatus? doc) => doc != null && doc.IsFiscalDocument;

    private static bool IsDocAdvanced(LastEmittedDocStatus? doc, DocPosition baseline)
    {
        return IsComparable(doc) &&
            (doc!.ZNumber > baseline.ZNumber ||
             (doc.ZNumber == baseline.ZNumber && doc.DocNumber > baseline.DocNumber));
    }

    private ReceiptResponse ApplyRecoveredDoc(ReceiptResponse receiptResponse, LastEmittedDocStatus doc, string lotteryCode)
    {
        receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(new POSReceiptSignatureData
        {
            RTSerialNumber = doc.PrinterSN ?? "",
            RTZNumber = doc.ZNumber,
            RTDocNumber = doc.DocNumber,
            RTDocMoment = doc.DocumentDateTime,
            RTDocType = "POSRECEIPT",
            RTCodiceLotteria = lotteryCode,
            RTCustomerID = "",
        }).ToArray();
        _lastDoc.Value = new DocPosition(doc.ZNumber, doc.DocNumber);
        return receiptResponse;
    }

    private async Task<LastEmittedDocStatus?> ReadLastEmittedDocStatusAsync(string receiptReference)
    {
        try
        {
            var command = new PrinterCommand() { DirectIO = DirectIO.GetLastEmittedDocStatusCommand() };
            var content = SoapSerializer.Serialize(command);
            var response = await _httpClient.SendCommandAsync(content);
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterCommandResponse>(responseContent);
            var rawData = result?.CommandResponse?.ResponseData;
            _logger.LogDebug("Last emitted document query response: success={success}, printerStatus={status}, rawData={raw}",
                result?.Success, result?.CommandResponse?.PrinterStatus, rawData);
            return LastEmittedDocStatus.Parse(rawData);
        }
        // Any failure is answered with "unknown", never with an exception: this is a diagnostic read, and its
        // callers treat a missing answer as "the printer state could not be established", which is safe.
        catch (Exception e)
        {
            _logger.LogWarning(e, "({receiptreference}) Could not query the last emitted document from the printer.", receiptReference);
            return null;
        }
    }

    private async Task<ProcessResponse> ProcessUnspecifiedProtocolReceipt(ProcessRequest request)
    {
        try
        {
            // This path prints without recording the resulting Z#/Doc#, so the baseline can no longer be trusted.
            InvalidateLastDocBaseline("unspecified protocol receipt");
            var content = EpsonCommandFactory.PerformUnspecifiedProtocolReceipt(request.ReceiptRequest);
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var printerResponse = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);

            if (printerResponse?.Success == false)
            {
                var error = GetErrorInfo(printerResponse?.Code, printerResponse?.Status, printerResponse?.Receipt?.PrinterStatus)?.Info;
                request.ReceiptResponse.SetReceiptResponseErrored(error ?? "Failed to process unspecified protocol");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            await ResetPrinter();
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception e)
        {
            var errorInfo = Helpers.ExceptionInfo(e);
            request.ReceiptResponse.SetReceiptResponseErrored(errorInfo.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }

    private async Task<ProcessResponse> ProcessPerformReprint(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            request.ReceiptResponse.SetReceiptResponseErrored("Cannot refund receipt without references.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        // The reprint directIO (3098) response carries no receiptInfo block, so the serial number
        // must be read before the reprint. A failure afterwards would error an already printed document.
        if (string.IsNullOrEmpty(_serialnr))
        {
            _ = await GetRTInfoAsync();
        }

        FiscalReceiptResponse fiscalResponse;
        PrinterReceiptResponse result = null;
        try
        {
            if (!string.IsNullOrEmpty(_configuration.Password))
            {
                var loginResult = await LoginAsync();
                if (!loginResult.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {loginResult.StatusCode}, Content: {await loginResult.Content.ReadAsStringAsync()})");
                }
                using var loginResultresponseContent = await loginResult.Content.ReadAsStreamAsync();
                var loginprinterresult = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(loginResultresponseContent);
                var loginReceiptResponse = await SetReceiptResponse(loginprinterresult);
                if (!loginReceiptResponse.Success)
                {
                    request.ReceiptResponse.SetReceiptResponseErrored($"Unable to login to the Printer. Please check the configured password. (Details: {loginReceiptResponse.SSCDErrorInfo?.Info ?? ""})");
                    return new ProcessResponse
                    {
                        ReceiptResponse = request.ReceiptResponse
                    };
                }
            }

            var date = DateTime.Parse(referenceDateTime);
            // The reprint emits a documento gestionale of its own; the baseline taken before it no longer
            // describes the printer.
            InvalidateLastDocBaseline("reprint");
            var response = await PerformReprint(date.ToString("dd"), date.ToString("MM"), date.ToString("yy"), long.Parse(referenceDocNumber));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            fiscalResponse = fiscalReceiptResponse;
            await ResetPrinter();
        }
        catch (Exception e)
        {
            fiscalResponse = Helpers.ExceptionInfo(e);
        }

        if (!fiscalResponse.Success)
        {
            request.ReceiptResponse.SetReceiptResponseErrored(fiscalResponse.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        else
        {
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber ?? _serialnr ?? "",
                RTZNumber = fiscalResponse.ZRepNumber,
                RTDocNumber = fiscalResponse.ReceiptNumber,
                RTDocMoment = fiscalResponse.ReceiptDateTime,
                RTDocType = "Documento Gestionale",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data
                RTReferenceZNumber = long.Parse(referenceZNumber),
                RTReferenceDocNumber = long.Parse(referenceDocNumber),
                RTReferenceDocMoment = DateTime.Parse(referenceDateTime)
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
        }
        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    private async Task<ProcessResponse> ProcessRefundReceipt(ProcessRequest request)
    {
        var referenceZNumberString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumberString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTimeString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        string referenceSerialnumber;
        DateTime referenceDateTime;
        if (string.IsNullOrEmpty(request.ReceiptRequest.cbPreviousReceiptReference))
        {
            // Use default values for unreferenced refunds
            referenceZNumberString = "0000";
            referenceDocNumberString = "0000";
            referenceDateTime = request.ReceiptRequest.cbReceiptMoment;
            referenceSerialnumber = "ND";
        }
        else
        {
            if (string.IsNullOrEmpty(referenceZNumberString) || string.IsNullOrEmpty(referenceDocNumberString) || string.IsNullOrEmpty(referenceDateTimeString))
            {
                request.ReceiptResponse.SetReceiptResponseErrored("Cannot refund receipt without references.");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            else
            {
                if (string.IsNullOrEmpty(_serialnr))
                {
                    _ = await GetRTInfoAsync();
                }

                referenceSerialnumber = _serialnr;
                referenceDateTime = DateTime.Parse(referenceDateTimeString);
            }
        }

        var content = EpsonCommandFactory.CreateRefundRequestContent(_configuration, request.ReceiptRequest, long.Parse(referenceDocNumberString), long.Parse(referenceZNumberString), referenceDateTime, referenceSerialnumber);
        // A refund emits a fiscal document too. Drop the baseline before sending and set it again from the
        // response: leaving the old value in place would credit the refund's Doc# to the next failing receipt.
        InvalidateLastDocBaseline("refund");
        try
        {
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }

            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber ?? _serialnr ?? "",
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "REFUND",
                RTCodiceLotteria = "",
                RTCustomerID = GetCustomerTaxId(request.ReceiptRequest),
                RTReferenceZNumber = long.Parse(referenceZNumberString),
                RTReferenceDocNumber = long.Parse(referenceDocNumberString),
                RTReferenceDocMoment = referenceDateTime
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            _lastDoc.Value = new DocPosition(fiscalReceiptResponse.ZRepNumber, fiscalReceiptResponse.ReceiptNumber);
            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                request.ReceiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception e)
        {
            var errorInfo = Helpers.ExceptionInfo(e);
            _logger.LogError(e, "Error while processing refund receipt: {error}", errorInfo.SSCDErrorInfo?.Info);
            request.ReceiptResponse.SetReceiptResponseErrored(errorInfo.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }

    private async Task<ProcessResponse> ProcessVoidReceipt(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            request.ReceiptResponse.SetReceiptResponseErrored($"The given cbPreviousReceiptReference '{request.ReceiptRequest.cbPreviousReceiptReference}' does not reference a request with RT references.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        if (string.IsNullOrEmpty(_serialnr))
        {
            _ = await GetRTInfoAsync();
        }
        var content = EpsonCommandFactory.CreateVoidRequestContent(_configuration, request.ReceiptRequest, long.Parse(referenceDocNumber), long.Parse(referenceZNumber), DateTime.Parse(referenceDateTime), _serialnr);
        // Same as the refund: a void emits its own fiscal document and moves the counter.
        InvalidateLastDocBaseline("void");
        try
        {
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber ?? _serialnr ?? "",
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "VOID",
                RTCodiceLotteria = "",
                RTCustomerID = GetCustomerTaxId(request.ReceiptRequest),
                RTReferenceZNumber = long.Parse(referenceZNumber),
                RTReferenceDocNumber = long.Parse(referenceDocNumber),
                RTReferenceDocMoment = DateTime.Parse(referenceDateTime)
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            _lastDoc.Value = new DocPosition(fiscalReceiptResponse.ZRepNumber, fiscalReceiptResponse.ReceiptNumber);

            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                request.ReceiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception e)
        {
            var errorInfo = Helpers.ExceptionInfo(e);
            _logger.LogError(e, "Error while processing void receipt: {error}", errorInfo.SSCDErrorInfo?.Info);
            request.ReceiptResponse.SetReceiptResponseErrored(errorInfo.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }

    private async Task<string> GetSerialNumberAsync(string rtType)
    {
        var serialQuery = new PrinterCommand() { DirectIO = DirectIO.GetSerialNrCommand() };
        var content = SoapSerializer.Serialize(serialQuery);
        var responseSerialnr = await _httpClient.SendCommandAsync(content);

        using var responseContent = await responseSerialnr.Content.ReadAsStreamAsync();
        var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterCommandResponse>(responseContent);

        var serialnr = result?.CommandResponse?.ResponseData;
        return serialnr?.Substring(10, 2) + rtType + serialnr?.Substring(8, 2) + serialnr?.Substring(2, 6);
    }

    private async Task ResetPrinter()
    {
        var resetCommand = new PrinterCommand() { ResetPrinter = new ResetPrinter() { Operator = "" } };
        var xml = SoapSerializer.Serialize(resetCommand);
        await _httpClient.SendCommandAsync(xml);
    }

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptResponse receiptResponse)
    {
        // A Z report moves the printer to a new Z and restarts the document counter, so the baseline taken
        // during the closed day would compare against a different numbering. The response does carry a
        // zRepNumber, but whether it names the day just closed or the one just opened cannot be established
        // from the protocol documentation available here — and reading it as the closed day would make the
        // first receipt of the new day look "advanced" and recover a document that is not its own. Dropping
        // the baseline costs one extra status read on the next receipt and is right either way.
        InvalidateLastDocBaseline("daily closing");

        try
        {
            var fiscalReport = new FiscalReport
            {
                ZReport = new ZReport()
            };
            var response = await _httpClient.SendCommandAsync(SoapSerializer.Serialize(fiscalReport));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<ReportResponse>(responseContent);
            if (!result?.Success ?? false)
            {
                var errorInfo = GetErrorInfo(result?.Code, result?.Status, null);
                await ResetPrinter();
                receiptResponse.SetReceiptResponseErrored(errorInfo.Info);
                return receiptResponse;
            }
            var zRepNumber = result?.ReportInfo?.ZRepNumber != null ? long.Parse(result.ReportInfo.ZRepNumber) : 0;
            receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(zRepNumber);
            if (result?.ReportInfo?.PrinterStatus != null && !result.ReportInfo.PrinterStatus.StartsWith("0"))
            {
                receiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.ReportInfo?.PrinterStatus) ?? "");
            }
            if (_configuration.ForceRebootAfterDailyClosing)
            {
                // #549: the printer sometimes gets stuck during the day; a post-closing reboot clears it.
                await SendRebootCommandAsync();
            }
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    private async Task<ProcessResponse> PerformZeroReceiptOperationAsync(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        if (request.IsRebootRequest())
        {
            return await PerformRebootAsync(receiptResponse);
        }
        await ResetPrinter();
        var result = await QueryPrinterStatusAsync();
        var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
        if (request.IsXReportZeroReceipt())
        {
            var fiscalReport = new FiscalReport
            {
                XReport = new XReport()
            };
            var response = await _httpClient.SendCommandAsync(SoapSerializer.Serialize(fiscalReport));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var reportResponse = SoapSerializer.DeserializeToSoapEnvelope<ReportResponse>(responseContent);
            if (!(result?.Success ?? false))
            {
                var errorInfo = GetErrorInfo(result?.Code, result?.Status, null);
                await ResetPrinter();
                receiptResponse.SetReceiptResponseErrored(errorInfo.Info);
            }
        }
        var stateData = JsonConvert.SerializeObject(new
        {
            PrinterStatus = result
        });
        return ProcessResponseHelpers.CreateResponse(receiptResponse, stateData, signatures);
    }

    private async Task<ProcessResponse> PerformRebootAsync(ReceiptResponse receiptResponse)
    {
        await SendRebootCommandAsync();
        return ProcessResponseHelpers.CreateResponse(receiptResponse, SignatureFactory.CreateZeroReceiptSignatures().ToList());
    }

    private async Task SendRebootCommandAsync()
    {
        try
        {
            // Sent immediately; a directIO restart blocks the response, so the printer reboots without replying.
            await _httpClient.SendCommandAsync(EpsonCommandFactory.RebootCommand());
        }
        catch (Exception e)
        {
            // ponytail: reboot drops the connection before answering (protocol note [3]: FP_NO_ANSWER) — expected, not an error.
            _logger.LogInformation(e, "Reboot command sent; the printer restarts without responding.");
        }
    }

    private async Task<HttpResponseMessage> LoginAsync() => await _httpClient.SendCommandAsync(EpsonCommandFactory.LoginCommand(_configuration.Password));

    private async Task<HttpResponseMessage> PerformReprint(string day, string month, string year, long receiptNumber) => await _httpClient.SendCommandAsync(EpsonCommandFactory.ReprintCommand(day, month, year, receiptNumber));

    public SSCDErrorInfo GetErrorInfo(string? code, string? status, string? printerStatus)
    {
        var errorInf = string.Empty;
        if (code != null)
        {
            errorInf += $"\n Error Code {code}: {_errorCodeFactory.GetCodeInfo(code)} ";
        }
        if (status != null)
        {
            errorInf += $"\n Status {status}: {_errorCodeFactory.GetStatusInfo(int.Parse(status))}";
        }
        var state = Helpers.GetPrinterStatus(printerStatus);
        if (state != null)
        {
            errorInf += $"\n Printer state {state}";
        }
        _logger.LogError(errorInf);
        return new SSCDErrorInfo() { Info = errorInf, Type = SSCDErrorType.Device };
    }
}
