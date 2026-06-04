using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Clients;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter;

public sealed class CustomRTPrinterSCU : LegacySCU
{
    private readonly ILogger<CustomRTPrinterSCU> _logger;
    private readonly CustomRTPrinterClient _printerClient;
    private readonly CustomRTPrinterConfiguration _configuration;
    private string _serialnr;

    public CustomRTPrinterSCU(ILogger<CustomRTPrinterSCU> logger, ILoggerFactory loggerFactory, CustomRTPrinterConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _printerClient = new CustomRTPrinterClient(configuration, loggerFactory.CreateLogger<CustomRTPrinterClient>());
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public override async Task<RTInfo> GetRTInfoAsync()
    {
        try
        {
            var info = await _printerClient.SendCommand<InfoResp>(new GetInfo());
            if (!string.IsNullOrEmpty(info.SerialNumber))
            {
                _serialnr = info.SerialNumber;
                if (string.IsNullOrEmpty(_configuration.Username))
                    _printerClient.SetBasicAuth(_serialnr, _serialnr);
            }
            return new RTInfo
            {
                InfoData = JsonConvert.SerializeObject(info),
                SerialNumber = _serialnr ?? ""
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetRTInfoAsync: failed to contact printer at {DeviceUrl}", _configuration.DeviceUrl);
            return new RTInfo
            {
                SerialNumber = _serialnr ?? "",
                InfoData = JsonConvert.SerializeObject(new { error = e.Message })
            };
        }
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            var receiptCase = request.ReceiptRequest.GetReceiptCase();
            if (string.IsNullOrEmpty(_serialnr))
            {
                var info = await _printerClient.SendCommand<InfoResp>(new GetInfo());
                _logger.LogInformation("{info}", JsonConvert.SerializeObject(info));
                _serialnr = info.SerialNumber;
                if (string.IsNullOrEmpty(_configuration.Username) && !string.IsNullOrEmpty(_serialnr))
                    _printerClient.SetBasicAuth(_serialnr, _serialnr);
            }

            if (request.ReceiptRequest.IsInitialOperationReceipt())
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateInitialOperationSignatures().ToList());

            if (request.ReceiptRequest.IsOutOfOperationReceipt())
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateOutOfOperationSignatures().ToList());

            if (request.ReceiptRequest.IsZeroReceipt())
                return await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse);

            if (request.ReceiptRequest.IsVoid())
                return await ProcessVoidReceiptAsync(request);

            if (request.ReceiptRequest.IsRefund())
                return await ProcessRefundReceiptAsync(request);

            if (request.ReceiptRequest.IsDailyClosing() || request.ReceiptRequest.IsMonthlyClosing() || request.ReceiptRequest.IsYearlyClosing())
                return CreateResponse(await PerformDailyCosing(request.ReceiptResponse));

            if (request.ReceiptRequest.IsReprint())
                return await ProcessReprintAsync(request);

            if (receiptCase == (long)ITReceiptCases.ProtocolUnspecified0x3000 && (request.ReceiptRequest.ftReceiptCase & 0x0000_0002_0000_0000) != 0)
                return await ProcessNonFiscalAsync(request);

            switch (receiptCase)
            {
                case (long)ITReceiptCases.UnknownReceipt0x0000:
                case (long)ITReceiptCases.PointOfSaleReceipt0x0001:
                    return CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
                case (long)ITReceiptCases.DeliveryNote0x0005:
                    return CreateResponse(await PerformDeliveryNoteAsync(request.ReceiptRequest, request.ReceiptResponse));
                case (long)ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003:
                    return await ProcessNonFiscalAsync(request);
            }

            request.ReceiptResponse.SetReceiptResponseErrored($"The given receiptcase 0x{receiptCase:X} is not supported by Custom RT Printer.");
            return CreateResponse(request.ReceiptResponse);
        }
        catch (Exception ex)
        {
            var signatures = new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "customrt-printer-generic-error",
                    Data = $"{ex}",
                    ftSignatureFormat = (long)SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954_2000_0000_3000
                }
            };
            request.ReceiptResponse.ftState |= 0xEEEE_EEEE;
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
        }
    }

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptResponse receiptResponse)
    {
        try
        {
            await EnsurePrinterReadyAsync();
            var response = await _printerClient.SendFiscalReport<Response<InfoResp>>(new PrintZReport());
            if (!response.Success)
            {
                receiptResponse.SetReceiptResponseErrored($"Printer error during daily closing. Status: {response.Status}");
                return receiptResponse;
            }

            // Z-report response often omits <nClose> — fall back to querying GetInfo for the post-closing ZSetNumber.
            long zNumber;
            if (long.TryParse(response.AddInfo?.NClose, out var z))
            {
                zNumber = z;
            }
            else
            {
                var info = await _printerClient.SendCommand<InfoResp>(new GetInfo());
                zNumber = info.ZSetNumber;
                _logger.LogDebug("Z-report response had no nClose, queried GetInfo: zSetNumber={Z}", zNumber);
            }

            receiptResponse.ftState = ITConstants.BASE_STATE;
            receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(zNumber);
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    public async Task<ReceiptResponse> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            await EnsurePrinterReadyAsync();
            var records = new List<IFiscalRecord>();

            var lotteryData = receiptRequest.GetLotteryData();
            var lotteryCode = lotteryData?.servizi_lotteriadegliscontrini_gov_it?.codicelotteria ?? "";
            if (!string.IsNullOrEmpty(lotteryCode))
                records.Add(new SetLotteryCode { Code = lotteryCode });

            var customer = receiptRequest.GetCustomer();

            foreach (var c in receiptRequest.cbChargeItems)
                _logger.LogDebug("ChargeItem '{Desc}' ftChargeItemCase=0x{Case:X16} amount={Amt} qty={Qty} → discount={D} surcharge={S}",
                    c.Description, c.ftChargeItemCase, c.Amount, c.Quantity, c.IsSubtotalDiscount(), c.IsSubtotalSurcharge());

            var lineItems = receiptRequest.cbChargeItems.Where(c => !c.IsSubtotalDiscount() && !c.IsSubtotalSurcharge()).ToList();
            var subtotalAdjustments = receiptRequest.cbChargeItems.Where(c => c.IsSubtotalDiscount() || c.IsSubtotalSurcharge()).ToList();

            foreach (var c in lineItems)
            {
                if (c.IsTip() || c.IsMultiUseVoucher())
                {
                    // Tip / multi-use voucher → printed as item on a non-VAT department (programmed slot 11 on Custom RT, mirrors Epson).
                    records.Add(new PrintRecItem
                    {
                        Description = c.Description,
                        Quantity = Math.Abs(c.Quantity) * 1000,
                        UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Round(Math.Abs(c.Amount) / Math.Abs(c.Quantity) * 100),
                        Department = TipVoucherDepartment
                    });
                }
                else if (c.IsSingleUseVoucher() && c.Amount < 0)
                {
                    // Single-use voucher used as payment → discount adjustment on the last item.
                    records.Add(new PrintRecItemAdjustment
                    {
                        AdjustmentType = 3,
                        Description = c.Description,
                        Amount = Math.Round(Math.Abs(c.Amount) * 100),
                        IdVat = GetIdVat(c.ftChargeItemCase),
                        Department = 1
                    });
                }
                else if (c.Amount < 0)
                {
                    // Item-level discount on the previously printed item (adjustmentType=3 = amount-based discount).
                    records.Add(new PrintRecItemAdjustment
                    {
                        AdjustmentType = 3,
                        Description = c.Description,
                        Amount = Math.Round(Math.Abs(c.Amount) * 100),
                        IdVat = GetIdVat(c.ftChargeItemCase),
                        Department = 1
                    });
                }
                else
                {
                    records.Add(new PrintRecItem
                    {
                        Description = c.Description,
                        Quantity = Math.Abs(c.Quantity) * 1000,
                        UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Round(Math.Abs(c.Amount) / Math.Abs(c.Quantity) * 100),
                        IdVat = GetIdVat(c.ftChargeItemCase)
                    });
                }
            }

            if (subtotalAdjustments.Any())
            {
                foreach (var adj in subtotalAdjustments)
                {
                    records.Add(new PrintRecSubtotalAdjustment
                    {
                        AdjustmentType = adj.IsSubtotalDiscount() ? 3u : 4u, // 3=sconto importo, 4=maggiorazione importo (su subtotale, in centesimi)
                        // Printer rejects long/complex descriptions on printRecSubtotalAdjustment (status=7); cap at 20 chars.
                        Description = (adj.Description ?? "").Length > 20 ? adj.Description.Substring(0, 20) : adj.Description,
                        Amount = Math.Round(Math.Abs(adj.Amount) * 100)
                    });
                }
                records.Add(new PrintRecSubtotal());
            }

            // Customer fiscal code / VAT (scontrino parlante) — goes AFTER items/subtotal and BEFORE printRecTotal.
            var customerCfOrVat = customer?.CustomerId ?? customer?.CustomerVATId;
            if (!string.IsNullOrEmpty(customerCfOrVat))
                records.Add(new FixedLines { Pitch = "B", Description = customerCfOrVat.Trim().ToUpperInvariant() });

            if (receiptRequest.cbPayItems?.Any() == true)
            {
                records.AddRange(receiptRequest.cbPayItems.Select(p => new PrintRecTotal
                {
                    Description = p.Description,
                    Payment = Math.Round(Math.Abs(p.Amount) * 100),
                    PaymentType = GetPaymentType(p.ftPayItemCase)
                }));
            }
            else
            {
                records.Add(new PrintRecTotal
                {
                    Description = "Pagamento",
                    Payment = Math.Round(Math.Abs(receiptRequest.cbReceiptAmount ?? 0) * 100),
                    PaymentType = 1
                });
            }

            var response = await _printerClient.SendFiscalReceipt<Response<InfoResp>>(records.ToArray());

            long docZNumber, docNumber;
            DateTime docMoment;
            if (!response.Success && response.Status == PdfBusyStatus)
            {
                _logger.LogWarning("Status=76 (PDF busy) after receipt send — querying last fiscal document.");
                var (dn, zn, dm) = await GetLastFiscalDocAsync();
                docNumber = dn; docZNumber = zn; docMoment = dm;
            }
            else if (!response.Success)
            {
                await ResetPrinter();
                receiptResponse.SetReceiptResponseErrored($"Printer error during receipt. Status: {response.Status}");
                return receiptResponse;
            }
            else
            {
                docZNumber = long.TryParse(response.AddInfo?.NClose, out var zn) ? zn : 0;
                docNumber  = long.TryParse(response.AddInfo?.FiscalDoc, out var fd) ? fd : 0;
                docMoment  = response.AddInfo?.DateTime ?? DateTime.UtcNow;
            }

            receiptResponse.ftState = ITConstants.BASE_STATE;
            var posReceiptSignature = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = docZNumber,
                RTDocNumber = docNumber,
                RTDocMoment = docMoment,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = lotteryCode,
                RTCustomerID = customer?.CustomerId ?? ""
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignature).ToArray();
            return receiptResponse;
        }
        catch (Exception e)
        {
            await ResetPrinter();
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    private async Task<ProcessResponse> PerformZeroReceiptOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            await ResetPrinter();
            var statusResponse = await _printerClient.SendCommand<Response<InfoResp>>(new QueryPrinterStatus());

            if (receiptRequest.IsXReportZeroReceipt())
                await _printerClient.SendFiscalReport<Response<InfoResp>>(new PrintXReport());

            var stateData = JsonConvert.SerializeObject(new
            {
                PrinterStatus = statusResponse.AddInfo?.PrinterStatus,
                FpStatus = statusResponse.AddInfo?.FpStatus
            });
            var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
            return ProcessResponseHelpers.CreateResponse(receiptResponse, stateData, signatures);
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return CreateResponse(receiptResponse);
        }
    }

    private async Task<ProcessResponse> ProcessVoidReceiptAsync(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTimeString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;

        var records = new List<IFiscalRecord>();
        if (request.ReceiptRequest.cbChargeItems?.Any() == true)
        {
            foreach (var c in request.ReceiptRequest.cbChargeItems)
                _logger.LogDebug("Void item '{Desc}' ftChargeItemCase=0x{Case:X16} → idVat={IdVat}", c.Description, c.ftChargeItemCase, GetIdVat(c.ftChargeItemCase));

            records.AddRange(request.ReceiptRequest.cbChargeItems.Select(c => new PrintRecItem
            {
                Description = c.Description,
                Quantity = Math.Abs(c.Quantity) * 1000,
                UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Round(Math.Abs(c.Amount) / Math.Abs(c.Quantity) * 100),
                IdVat = GetIdVat(c.ftChargeItemCase)
            }));
        }

        await EnsurePrinterReadyAsync();
        try
        {
            Response<InfoResp> response;
            DateTime refDate;

            if (string.IsNullOrEmpty(request.ReceiptRequest.cbPreviousReceiptReference)
                || string.IsNullOrEmpty(referenceZNumber)
                || string.IsNullOrEmpty(referenceDocNumber)
                || string.IsNullOrEmpty(referenceDateTimeString))
            {
                refDate = request.ReceiptRequest.cbReceiptMoment;
                var beginSpecial = new BeginRtDocAnnulmentSpecial { DocDate = refDate.ToString("ddMMyy") };
                var annulmentSpecial = new PrinterFiscalReceiptAnnulmentSpecial(beginSpecial, records.ToArray());
                response = await _printerClient.SendAsync<PrinterFiscalReceiptAnnulmentSpecial, Response<InfoResp>>(annulmentSpecial);
                referenceZNumber = "0";
                referenceDocNumber = "0";
            }
            else
            {
                refDate = DateTime.Parse(referenceDateTimeString);
                var begin = new BeginRtDocAnnulment
                {
                    DocRefZ = referenceZNumber,
                    DocRefNumber = referenceDocNumber,
                    DocDate = refDate.ToString("ddMMyy"),
                    FiscalSerial = _serialnr
                };
                var annulment = new PrinterFiscalReceiptAnnulment(begin, records.ToArray());
                response = await _printerClient.SendAsync<PrinterFiscalReceiptAnnulment, Response<InfoResp>>(annulment);
            }

            long voidZNumber, voidDocNumber;
            DateTime voidDocMoment;
            if (!response.Success && response.Status == PdfBusyStatus)
            {
                _logger.LogWarning("Status=76 (PDF busy) after void send — querying last fiscal document.");
                var (dn, zn, dm) = await GetLastFiscalDocAsync();
                voidDocNumber = dn; voidZNumber = zn; voidDocMoment = dm;
            }
            else if (!response.Success)
            {
                await ResetPrinter();
                request.ReceiptResponse.SetReceiptResponseErrored($"Printer error during void. Status: {response.Status}");
                return CreateResponse(request.ReceiptResponse);
            }
            else
            {
                voidZNumber   = long.TryParse(response.AddInfo?.NClose, out var vzn) ? vzn : 0;
                voidDocNumber = long.TryParse(response.AddInfo?.FiscalDoc, out var vfd) ? vfd : 0;
                voidDocMoment = response.AddInfo?.DateTime ?? DateTime.UtcNow;
            }

            request.ReceiptResponse.ftState = ITConstants.BASE_STATE;
            var posReceiptSignature = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = voidZNumber,
                RTDocNumber = voidDocNumber,
                RTDocMoment = voidDocMoment,
                RTDocType = "VOID",
                RTCodiceLotteria = "",
                RTCustomerID = "",
                RTReferenceZNumber = long.TryParse(referenceZNumber, out var rvzn) ? rvzn : 0,
                RTReferenceDocNumber = long.TryParse(referenceDocNumber, out var rvdn) ? rvdn : 0,
                RTReferenceDocMoment = refDate
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignature).ToArray();
            return CreateResponse(request.ReceiptResponse);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing void receipt");
            await ResetPrinter();
            request.ReceiptResponse.SetReceiptResponseErrored(e.Message);
            return CreateResponse(request.ReceiptResponse);
        }
    }

    private async Task<ProcessResponse> ProcessRefundReceiptAsync(ProcessRequest request)
    {
        var referenceZNumberString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumberString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTimeString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;

        var records = new List<IFiscalRecord>();
        foreach (var c in request.ReceiptRequest.cbChargeItems)
            _logger.LogDebug("Refund item '{Desc}' ftChargeItemCase=0x{Case:X16} → idVat={IdVat}", c.Description, c.ftChargeItemCase, GetIdVat(c.ftChargeItemCase));

        records.AddRange(request.ReceiptRequest.cbChargeItems.Select(c => new PrintRecItem
        {
            Description = c.Description,
            Quantity = Math.Abs(c.Quantity) * 1000,
            UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Round(Math.Abs(c.Amount) / Math.Abs(c.Quantity) * 100),
            IdVat = GetIdVat(c.ftChargeItemCase)
        }));

        await EnsurePrinterReadyAsync();
        try
        {
            Response<InfoResp> response;
            DateTime referenceDateTime;

            if (string.IsNullOrEmpty(request.ReceiptRequest.cbPreviousReceiptReference)
                || string.IsNullOrEmpty(referenceZNumberString)
                || string.IsNullOrEmpty(referenceDocNumberString)
                || string.IsNullOrEmpty(referenceDateTimeString))
            {
                referenceDateTime = request.ReceiptRequest.cbReceiptMoment;
                var beginSpecial = new BeginRtDocRefundSpecial { DocDate = referenceDateTime.ToString("ddMMyy") };
                var refundSpecial = new PrinterFiscalReceiptRefundSpecial(beginSpecial, records.ToArray());
                response = await _printerClient.SendAsync<PrinterFiscalReceiptRefundSpecial, Response<InfoResp>>(refundSpecial);
                referenceZNumberString = "0";
                referenceDocNumberString = "0";
            }
            else
            {
                referenceDateTime = DateTime.Parse(referenceDateTimeString);
                var begin = new BeginRtDocRefund
                {
                    DocRefZ = referenceZNumberString,
                    DocRefNumber = referenceDocNumberString,
                    DocDate = referenceDateTime.ToString("ddMMyy"),
                    FiscalSerial = _serialnr
                };
                var refund = new PrinterFiscalReceiptRefund(begin, records.ToArray());
                response = await _printerClient.SendAsync<PrinterFiscalReceiptRefund, Response<InfoResp>>(refund);
            }

            long refZNumber, refDocNumber;
            DateTime refDocMoment;
            if (!response.Success && response.Status == PdfBusyStatus)
            {
                _logger.LogWarning("Status=76 (PDF busy) after refund send — querying last fiscal document.");
                var (dn, zn, dm) = await GetLastFiscalDocAsync();
                refDocNumber = dn; refZNumber = zn; refDocMoment = dm;
            }
            else if (!response.Success)
            {
                await ResetPrinter();
                request.ReceiptResponse.SetReceiptResponseErrored($"Printer error during refund. Status: {response.Status}");
                return CreateResponse(request.ReceiptResponse);
            }
            else
            {
                refZNumber   = long.TryParse(response.AddInfo?.NClose, out var rzn) ? rzn : 0;
                refDocNumber = long.TryParse(response.AddInfo?.FiscalDoc, out var rfd) ? rfd : 0;
                refDocMoment = response.AddInfo?.DateTime ?? DateTime.UtcNow;
            }

            request.ReceiptResponse.ftState = ITConstants.BASE_STATE;
            var posReceiptSignature = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = refZNumber,
                RTDocNumber = refDocNumber,
                RTDocMoment = refDocMoment,
                RTDocType = "REFUND",
                RTCodiceLotteria = "",
                RTCustomerID = "",
                RTReferenceZNumber = long.TryParse(referenceZNumberString, out var rz) ? rz : 0,
                RTReferenceDocNumber = long.TryParse(referenceDocNumberString, out var rd) ? rd : 0,
                RTReferenceDocMoment = referenceDateTime
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignature).ToArray();
            return CreateResponse(request.ReceiptResponse);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing refund receipt");
            await ResetPrinter();
            request.ReceiptResponse.SetReceiptResponseErrored(e.Message);
            return CreateResponse(request.ReceiptResponse);
        }
    }

    public async Task<ReceiptResponse> PerformDeliveryNoteAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            await EnsurePrinterReadyAsync();
            var records = new List<IFiscalRecord>();

            var customer = receiptRequest.GetCustomer();
            if (customer != null)
            {
                if (!string.IsNullOrEmpty(customer.CustomerName))
                    records.Add(new PrintRecItem { Description = customer.CustomerName, UnitPrice = 0, IdVat = 1 });
                if (!string.IsNullOrEmpty(customer.CustomerStreet))
                    records.Add(new PrintRecItem { Description = customer.CustomerStreet, UnitPrice = 0, IdVat = 1 });
                if (!string.IsNullOrEmpty(customer.CustomerCity))
                    records.Add(new PrintRecItem { Description = $"{customer.CustomerZip} {customer.CustomerCity}".Trim(), UnitPrice = 0, IdVat = 1 });
            }

            var lineItems = receiptRequest.cbChargeItems.Where(c => !c.IsSubtotalDiscount() && !c.IsSubtotalSurcharge()).ToList();
            var subtotalAdjustments = receiptRequest.cbChargeItems.Where(c => c.IsSubtotalDiscount() || c.IsSubtotalSurcharge()).ToList();

            foreach (var c in lineItems)
            {
                if (c.IsTip() || c.IsMultiUseVoucher())
                {
                    records.Add(new PrintRecItem
                    {
                        Description = c.Description,
                        Quantity = Math.Abs(c.Quantity) * 1000,
                        UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Round(Math.Abs(c.Amount) / Math.Abs(c.Quantity) * 100),
                        Department = TipVoucherDepartment
                    });
                }
                else if (c.IsSingleUseVoucher() && c.Amount < 0)
                {
                    records.Add(new PrintRecItemAdjustment
                    {
                        AdjustmentType = 3,
                        Description = c.Description,
                        Amount = Math.Round(Math.Abs(c.Amount) * 100),
                        IdVat = GetIdVat(c.ftChargeItemCase),
                        Department = 1
                    });
                }
                else if (c.Amount < 0)
                {
                    records.Add(new PrintRecItemAdjustment
                    {
                        AdjustmentType = 3,
                        Description = c.Description,
                        Amount = Math.Round(Math.Abs(c.Amount) * 100),
                        IdVat = GetIdVat(c.ftChargeItemCase),
                        Department = 1
                    });
                }
                else
                {
                    records.Add(new PrintRecItem
                    {
                        Description = c.Description,
                        Quantity = Math.Abs(c.Quantity) * 1000,
                        UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Round(Math.Abs(c.Amount) / Math.Abs(c.Quantity) * 100),
                        IdVat = GetIdVat(c.ftChargeItemCase)
                    });
                }
            }

            if (subtotalAdjustments.Any())
            {
                foreach (var adj in subtotalAdjustments)
                {
                    records.Add(new PrintRecSubtotalAdjustment
                    {
                        AdjustmentType = adj.IsSubtotalDiscount() ? 3u : 4u, // 3=sconto importo, 4=maggiorazione importo (su subtotale, in centesimi)
                        // Printer rejects long/complex descriptions on printRecSubtotalAdjustment (status=7); cap at 20 chars.
                        Description = (adj.Description ?? "").Length > 20 ? adj.Description.Substring(0, 20) : adj.Description,
                        Amount = Math.Round(Math.Abs(adj.Amount) * 100)
                    });
                }
                records.Add(new PrintRecSubtotal());
            }

            var deliveryCfOrVat = customer?.CustomerId ?? customer?.CustomerVATId;
            if (!string.IsNullOrEmpty(deliveryCfOrVat))
                records.Add(new FixedLines { Pitch = "B", Description = deliveryCfOrVat.Trim().ToUpperInvariant() });

            if (receiptRequest.cbPayItems?.Any() == true)
            {
                records.AddRange(receiptRequest.cbPayItems.Select(p => new PrintRecTotal
                {
                    Description = p.Description,
                    Payment = Math.Round(Math.Abs(p.Amount) * 100),
                    PaymentType = GetPaymentType(p.ftPayItemCase)
                }));
            }
            else
            {
                records.Add(new PrintRecTotal
                {
                    Description = "Pagamento",
                    Payment = Math.Round(Math.Abs(receiptRequest.cbReceiptAmount ?? 0) * 100),
                    PaymentType = 1
                });
            }

            var response = await _printerClient.SendFiscalReceipt<Response<InfoResp>>(records.ToArray());

            if (!response.Success)
            {
                await ResetPrinter();
                receiptResponse.SetReceiptResponseErrored($"Printer error during delivery note. Status: {response.Status}");
                return receiptResponse;
            }

            receiptResponse.ftState = ITConstants.BASE_STATE;
            var posReceiptSignature = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = long.TryParse(response.AddInfo?.NClose, out var dzn) ? dzn : 0,
                RTDocNumber = long.TryParse(response.AddInfo?.FiscalDoc, out var dfd) ? dfd : 0,
                RTDocMoment = response.AddInfo?.DateTime ?? DateTime.UtcNow,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = "",
                RTCustomerID = customer?.CustomerId ?? ""
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignature).ToArray();
            return receiptResponse;
        }
        catch (Exception e)
        {
            await ResetPrinter();
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    private async Task<ProcessResponse> ProcessReprintAsync(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTimeString = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;

        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTimeString))
        {
            request.ReceiptResponse.SetReceiptResponseErrored($"Cannot reprint: missing reference data for '{request.ReceiptRequest.cbPreviousReceiptReference}'.");
            return CreateResponse(request.ReceiptResponse);
        }

        try
        {
            var refDate = DateTime.Parse(referenceDateTimeString!);
            // printDuplicateReceipt takes no parameters — always reprints the last fiscal document
            var response = await _printerClient.SendFiscalReport<Response<InfoResp>>(new PrintDuplicateReceipt());

            if (!response.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored($"Printer error during reprint. Status: {response.Status}");
                return CreateResponse(request.ReceiptResponse);
            }

            var posReceiptSignature = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = long.TryParse(referenceZNumber, out var rz) ? rz : 0,
                RTDocNumber = long.TryParse(referenceDocNumber, out var rd) ? rd : 0,
                RTDocMoment = refDate,
                RTDocType = "Documento Gestionale",
                RTCodiceLotteria = "",
                RTCustomerID = "",
                RTReferenceZNumber = long.TryParse(referenceZNumber, out var rrz) ? rrz : 0,
                RTReferenceDocNumber = long.TryParse(referenceDocNumber, out var rrd) ? rrd : 0,
                RTReferenceDocMoment = refDate
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignature).ToArray();
            return CreateResponse(request.ReceiptResponse);
        }
        catch (Exception e)
        {
            request.ReceiptResponse.SetReceiptResponseErrored(e.Message);
            return CreateResponse(request.ReceiptResponse);
        }
    }

    private async Task<ProcessResponse> ProcessNonFiscalAsync(ProcessRequest request)
    {
        try
        {
            var records = (request.ReceiptRequest.cbChargeItems ?? Array.Empty<ChargeItem>())
                .Select(c => (INonFiscalRecord)new PrintNormal { Data = c.Description })
                .ToArray();

            var response = await _printerClient.SendNonFiscal<Response<InfoResp>>(records);

            if (!response.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored($"Printer error during non-fiscal print. Status: {response.Status}");
                return CreateResponse(request.ReceiptResponse);
            }

            request.ReceiptResponse.ftSignatures = Array.Empty<SignaturItem>();
            return CreateResponse(request.ReceiptResponse);
        }
        catch (Exception e)
        {
            request.ReceiptResponse.SetReceiptResponseErrored(e.Message);
            return CreateResponse(request.ReceiptResponse);
        }
    }

    private async Task EnsurePrinterReadyAsync()
    {
        var status = await _printerClient.SendCommand<Response<InfoResp>>(new QueryPrinterStatus());
        _logger.LogDebug("Printer status: printerStatus={PS} fpStatus={FP} receiptStep={RS} mfStatus={MF}",
            status.AddInfo?.PrinterStatus, status.AddInfo?.FpStatus, status.AddInfo?.ReceiptStep, status.AddInfo?.MfStatus);

        if (status.AddInfo?.ReceiptStep != "0")
        {
            _logger.LogWarning("Printer has open document (receiptStep={RS}), resetting before new operation.", status.AddInfo?.ReceiptStep);
            await ResetPrinter();
            return;
        }

        if (status.AddInfo?.PrinterStatus != 0 || status.AddInfo?.FpStatus != 0)
            throw new CustomRTPrinterException($"Printer not ready: printerStatus={status.AddInfo?.PrinterStatus} fpStatus={status.AddInfo?.FpStatus} mfStatus={status.AddInfo?.MfStatus}");
    }

    private const int PdfBusyStatus = 76;

    // Department slot used on Custom RT for tip / multi-use voucher (mirrors Epson convention).
    // Must be programmed on the printer as non-VAT (esente/non soggetto) for tips and at the
    // appropriate rate for voucher use; if slot 11 isn't programmed, change this value.
    private const uint TipVoucherDepartment = 11;

    private async Task<(long DocNumber, long ZNumber, DateTime DocMoment)> GetLastFiscalDocAsync()
    {
        var response = await _printerClient.SendCommand<Response<string>>(new DirectIO { Command = "1017", Data = "" });
        var buf = response.AddInfo?.ResponseBuf ?? "";
        _logger.LogDebug("directIO 1017 responseBuf={Buf}", buf);

        if (buf.Length < 14)
            return (0, 0, DateTime.UtcNow);

        try
        {
            // Format: GG(2) MM(2) AA(2) HH(2) mm(2) NumeroScontrino(4) [ZNumber(4) ...]
            var day    = int.Parse(buf.Substring(0, 2));
            var month  = int.Parse(buf.Substring(2, 2));
            var year   = 2000 + int.Parse(buf.Substring(4, 2));
            var hour   = int.Parse(buf.Substring(6, 2));
            var minute = int.Parse(buf.Substring(8, 2));
            var docNumber = long.Parse(buf.Substring(10, 4));
            var zNumber   = buf.Length >= 18 ? long.Parse(buf.Substring(14, 4)) : 0;
            return (docNumber, zNumber, new DateTime(year, month, day, hour, minute, 0));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to parse directIO 1017 responseBuf '{Buf}'", buf);
            return (0, 0, DateTime.UtcNow);
        }
    }

    private async Task ResetPrinter()
    {
        // Best-effort cleanup of printer state. Must never throw — callers rely on this in catch blocks.
        try
        {
            var status = await _printerClient.SendCommand<Response<InfoResp>>(new QueryPrinterStatus());
            if (status.AddInfo?.ReceiptStep == "1")
            {
                try { _ = await _printerClient.CancelFiscalReceipt<Response<InfoResp>>(); }
                catch (Exception ex) { _logger.LogWarning(ex, "ResetPrinter: cancelFiscalReceipt failed (ignored)."); }
            }
            try { _ = await _printerClient.SendCommand<Response<InfoResp>>(new ResetPrinter()); }
            catch (Exception ex) { _logger.LogWarning(ex, "ResetPrinter: resetPrinter command failed (ignored)."); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ResetPrinter: queryPrinterStatus failed (ignored).");
        }
    }

    private static uint GetPaymentType(long ftPayItemCase) =>
        (ftPayItemCase & 0xFF) switch
        {
            0x01 => 1, // cash
            0x02 => 2, // card / non-cash
            0x03 => 2, // credit card
            0x08 => 3, // voucher / ticket
            _ => 1
        };

    private static uint GetIdVat(long ftChargeItemCase) =>
        (ftChargeItemCase & 0xF) switch
        {
            0x3 => 1, // Normal — 22%
            0x1 => 2, // Discounted-1 — 10%
            0x2 => 4, // Discounted-2 — 5%
            0x4 => 3, // Super reduced 1 — 4%
            _ => 1
        };

    public static ProcessResponse CreateResponse(ReceiptResponse receiptResponse) => new ProcessResponse { ReceiptResponse = receiptResponse };
}
