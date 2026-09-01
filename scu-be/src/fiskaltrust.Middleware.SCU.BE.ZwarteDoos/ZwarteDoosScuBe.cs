using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.be;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Helpers;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.ZwartedoosApi;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosScuBe : IBESSCD
{
    private readonly ZwarteDoosScuConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ZwarteDoosApiClient _zwarteDoosApiClient;
    private readonly ILogger<ZwarteDoosScuBe> _logger;

    public ZwarteDoosScuBe(ILogger<ZwarteDoosScuBe> logger, ILoggerFactory loggerFactory, ZwarteDoosScuConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler);
        _zwarteDoosApiClient = new ZwarteDoosApiClient(_configuration, _httpClient, loggerFactory.CreateLogger<ZwarteDoosApiClient>());
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.InitialOperationReceipt0x4001))
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateInitialOperationSignatures().ToList());
            }

            if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.OutOfOperationReceipt0x4002))
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateOutOfOperationSignatures().ToList());
            }

            if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.ZeroReceipt0x2000))
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, []);
            }

            if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.DailyClosing0x2011))
            {
                return ProcessResponseHelpers.CreateResponse(await PerformDailyCosing(request.ReceiptRequest, request.ReceiptResponse));
            }

            if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001))
            {
                return ProcessResponseHelpers.CreateResponse(await PerformPointOfSaleReceipt0x0001Async(request.ReceiptRequest, request.ReceiptResponse));
            }

            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, []);
        }
        catch (Exception ex)
        {
            request.ReceiptResponse.SetReceiptResponseErrored("zwartedoos-generic-error", ex.ToString());
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse);
        }
    }




    private async Task<ReceiptResponse> PerformPointOfSaleReceipt0x0001Async(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var baseData = GetBaseData(receiptRequest, receiptResponse);
            var signSalesRequest = new SaleInput
            {
                Language = baseData.Language,
                VatNo = baseData.VatNo,
                EstNo = baseData.EstNo,
                PosId = baseData.PosId,
                PosFiscalTicketNo = baseData.PosFiscalTicketNo,
                PosDateTime = baseData.PosDateTime,
                PosSwVersion = baseData.PosSwVersion,
                TerminalId = baseData.TerminalId,
                DeviceId = baseData.DeviceId,
                BookingPeriodId = baseData.BookingPeriodId,
                BookingDate = baseData.BookingDate,
                TicketMedium = baseData.TicketMedium,
                EmployeeId = baseData.EmployeeId,
                FdmRef = null,
                CostCenter = null,
                Transaction = GetTransactionInput(receiptRequest),
                Financials = GetFinancials(receiptRequest)
            };
            var apiResponse = await _zwarteDoosApiClient.SaleAsync(signSalesRequest, isTraining: receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training));
            receiptResponse.ftStateData = new
            {
                BE = new
                {
                    ZwarteDoos = new
                    {
                        ApiRequest = signSalesRequest,
                        ApiResponse = apiResponse
                    }
                }
            };
            if (apiResponse.Errors != null && apiResponse.Errors.Count > 0)
            {
                receiptResponse.SetReceiptResponseErrored(FormatApiErrors(apiResponse.Errors));
                return receiptResponse;
            }
            else
            {
                var signatures = new List<SignatureItem>
                {
                    new SignatureItem
                    {
                        Caption = "DigitalSignature",
                        Data = apiResponse!.Data!.SignResult!.DigitalSignature ?? "",
                        ftSignatureFormat = SignatureFormat.Text,
                        ftSignatureType = SignatureType.Unknown
                    },
                    new SignatureItem
                    {
                        Caption = "ShortSignature",
                        Data = apiResponse!.Data!.SignResult!.ShortSignature ?? "",
                        ftSignatureFormat = SignatureFormat.Text,
                        ftSignatureType = SignatureType.Unknown
                    },
                    new SignatureItem
                    {
                        Caption = "VerificationUrl",
                        Data = apiResponse!.Data!.SignResult!.VerificationUrl ?? "",
                        ftSignatureFormat = SignatureFormat.QRCode,
                        ftSignatureType = SignatureType.Unknown
                    }
                };
                receiptResponse.ftSignatures.AddRange(signatures);
            }
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    /// <summary>
    /// The FDM's human-readable message is deliberately vague ("De aanvraag gedaan aan de zwarte
    /// doos ... is ongeldig"); the actionable part is the <c>code</c> and the <c>SUBCODE</c> in the
    /// error's extensions (e.g. EXPECTED_NEGQUANTITYREASON, KEY_FIELD_COMBINATION_ALREADY_USED).
    /// Both are kept so the FAILURE signature names the rule the request broke.
    /// </summary>
    private static string FormatApiErrors(List<MessageItem> errors) => string.Join("; ", errors.Select(error =>
    {
        var details = new List<string>();
        if (error.Extensions is { } extensions)
        {
            if (!string.IsNullOrEmpty(extensions.Code))
            {
                details.Add(extensions.Code);
            }
            foreach (var item in extensions.Data ?? new List<DataItem>())
            {
                details.Add($"{item.Name}={item.Value}");
            }
        }
        return details.Count > 0 ? $"{error.Message} [{string.Join(", ", details)}]" : error.Message;
    }));

    private static List<PaymentLineInput> GetFinancials(ReceiptRequest receiptRequest)
    {
        var payments = receiptRequest.cbPayItems.Select(x => new PaymentLineInput
        {
            Id = x.Description,
            Name = x.Description,
            Type = Mappings.GetPaymentType(x),
            Provider = null, // Todo this shoudl maybe be read from the payitemcasedata body
            InputMethod = Models.Enums.InputMethod.MANUAL,  // Todo this shoudl maybe be read from the payitemcasedata body
            Amount = x.Amount,
            AmountType = Mappings.GetPaymentLineType(x),
            ForeignCurrency = null,
            Reference = null,  // Todo this shoudl maybe be read from the payitemcasedata body
            Drawer = null  // Todo this shoudl maybe be read from the payitemcasedata body
        });
        return payments.ToList();
    }

    internal TransactionInput GetTransactionInput(ReceiptRequest receiptRequest)
    {
        return new TransactionInput
        {
            TransactionLines = receiptRequest.cbChargeItems.Select(x => new TransactionLineInput
            {
                LineType = TransactionLineType.SINGLE_PRODUCT,
                MainProduct = new ProductInput
                {
                    Gtin = null,
                    ProductId = x.Description,
                    ProductName = x.Description,
                    DepartmentId = "default",
                    DepartmentName = "Default",
                    Quantity = x.Quantity,
                    QuantityType = QuantityType.PIECE,
                    NegQuantityReason = GetNegQuantityReason(receiptRequest, x),
                    // The FDM wants the price of ONE unit and expresses a reversal through the
                    // negative quantity and line total, so a refund line must still carry a
                    // POSITIVE unit price — a negative one is refused with VAT_PRICE_CHECK_FAILED.
                    // The quotient gives that for the well-formed case (amount and quantity share
                    // a sign) and is deliberately not Abs()'d: a POS that sends a negative amount
                    // against a positive quantity is describing something we should not silently
                    // reinterpret, and the FDM refusal names the field.
                    UnitPrice = x.Amount / x.Quantity,
                    Vats = [GetVatInputForChargeItem(x)]
                },
                SubProducts = [],
                CostCenter = null,
                LineTotal = x.Amount,
            }).ToList(),
            TransactionTotal = receiptRequest.cbChargeItems.Sum(x => x.Amount),
        };
    }

    /// <summary>
    /// The FDM refuses a negative quantity that does not say WHY it is negative
    /// (INVALID_REQUEST / EXPECTED_NEGQUANTITYREASON) — <see cref="ProductInput.NegQuantityReason"/>:
    /// "When the quantity is a negative amount the reason has to be specified."
    /// <para>
    /// Only the reason the fiskaltrust receipt model states outright is mapped: a charge item — or
    /// the whole receipt — flagged as a refund is a Belgian <c>REFUND</c>. The other Belgian reasons
    /// (<c>CORRECTION</c>, <c>WASTE</c>, <c>DAMAGE</c>) have no ft counterpart today, so an
    /// unflagged negative line is sent WITHOUT a reason and the FDM rejects it. That is on purpose:
    /// booking it under a guessed reason would misstate the negative-quantity totals of the Z
    /// report. Mapping the remaining reasons needs a product/fiscal decision.
    /// </para>
    /// </summary>
    private static string? GetNegQuantityReason(ReceiptRequest receiptRequest, ChargeItem chargeItem)
    {
        if (chargeItem.Quantity >= 0)
        {
            return null;
        }

        if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            return NegQuantityReason.REFUND.ToString();
        }

        return null;
    }

    public VatInput GetVatInputForChargeItem(ChargeItem chargeItem)
    {
        var vatAmount = Math.Round(chargeItem.Amount - (chargeItem.Amount / (1 + (chargeItem.VATRate / 100))), 2);
        return new VatInput
        {
            Label = Mappings.GetVatLabelForRate(chargeItem),
            Price = chargeItem.Amount,
            PriceChanges = []
        };
    }

    private BaseInputData GetBaseData(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var baseData = new BaseInputData
        {
            Language = _configuration.Language,
            VatNo = _configuration.VatNo,
            EstNo = _configuration.EstNo,
            PosId = receiptResponse.ftCashBoxIdentification,
            PosFiscalTicketNo = receiptResponse.ftQueueRow, // This maybe needs to change to ftReceiptNumber?
            PosDateTime = receiptRequest.cbReceiptMoment,
            PosSwVersion = "1.3.0",
            TerminalId = receiptResponse.cbTerminalID ?? receiptResponse.ftQueueID.ToString(),

            DeviceId = _configuration.DeviceId,
            BookingDate = DateOnly.FromDateTime(receiptRequest.cbReceiptMoment),
            BookingPeriodId = receiptResponse.ftQueueItemID,
            EmployeeId = GetEmployeeIdFromCbUser(receiptRequest.cbUser),
            TicketMedium = Models.Enums.TicketMedium.PAPER,
        };
        return baseData;
    }

    private string GetEmployeeIdFromCbUser(object? cbUser)
    {
        if (cbUser == null)
        {
            return "undefined";
        }

        // If it's already a string, return it
        if (cbUser is string userString)
        {
            return userString;
        }

        // Try to handle JsonElement or other object types that might contain a string value
        try
        {
            // Convert to string and try to parse as JSON first
            var userJson = cbUser.ToString();
            if (!string.IsNullOrEmpty(userJson))
            {
                // If it looks like JSON (starts with { or [), try to deserialize
                if (userJson.TrimStart().StartsWith("{") || userJson.TrimStart().StartsWith("["))
                {
                    // For now, just return the JSON string as-is
                    // In the future, you might want to parse and extract specific fields
                    return userJson;
                }
                else
                {
                    // It's a simple string value
                    return userJson;
                }
            }
        }
        catch
        {
            // If anything fails, fall back to undefined
        }

        return "undefined";
    }

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var baseData = GetBaseData(receiptRequest, receiptResponse);
            var reportTurnoverZInput = new ReportTurnoverZInput
            {
                BookingDate = baseData.BookingDate,
                BookingPeriodId = baseData.BookingPeriodId,
                DeviceId = baseData.DeviceId,
                EmployeeId = baseData.EmployeeId,
                EstNo = baseData.EstNo,
                Language = baseData.Language,
                PosDateTime = baseData.PosDateTime,
                PosFiscalTicketNo = baseData.PosFiscalTicketNo,
                PosId = baseData.PosId,
                PosSwVersion = baseData.PosSwVersion,
                TerminalId = baseData.TerminalId,
                TicketMedium = baseData.TicketMedium,
                VatNo = baseData.VatNo,
                FdmDevices = [],
                ReportBookingDate = baseData.BookingDate,
                ReportNo = receiptResponse.ftQueueRow,
                PosDevices = [],
                Turnover = new TurnoverInput
                {
                    Departments = [],
                    Invoices = [],
                    NegQuantities = [],
                    Payments = [],
                    PriceChanges = [],
                    Transactions = [],
                    Vats = [],
                    DrawersOpenCount = 0
                }
            };

            var apiResponse = await _zwarteDoosApiClient.ReportTurnoverZAsync(reportTurnoverZInput, isTraining: receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training));
            receiptResponse.ftStateData = new
            {
                BE = new
                {
                    ZwarteDoos = new
                    {
                        ApiRequest = reportTurnoverZInput,
                        ApiResponse = apiResponse
                    }
                }
            };
            if (apiResponse.Errors != null && apiResponse.Errors.Count > 0)
            {
                receiptResponse.SetReceiptResponseErrored(FormatApiErrors(apiResponse.Errors));
                return receiptResponse;
            }

            // A Z report the FDM did not sign is a failed daily closing, never a silent success.
            var signResult = apiResponse.Data?.SignResult;
            if (signResult == null)
            {
                receiptResponse.SetReceiptResponseErrored("The ZwarteDoos FDM returned no signResult for the daily closing (REPORT_TURNOVER_Z).");
                return receiptResponse;
            }

            var signatures = new List<SignatureItem>
            {
                new SignatureItem
                {
                    Caption = "DigitalSignature",
                    Data = signResult.DigitalSignature,
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureType.Unknown
                }
            };
            if (!string.IsNullOrEmpty(signResult.ShortSignature))
            {
                signatures.Add(new SignatureItem
                {
                    Caption = "ShortSignature",
                    Data = signResult.ShortSignature!,
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureType.Unknown
                });
            }
            if (!string.IsNullOrEmpty(signResult.VerificationUrl))
            {
                signatures.Add(new SignatureItem
                {
                    Caption = "VerificationUrl",
                    Data = signResult.VerificationUrl!,
                    ftSignatureFormat = SignatureFormat.QRCode,
                    ftSignatureType = SignatureType.Unknown
                });
            }
            receiptResponse.ftSignatures.AddRange(signatures);
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    public async Task<BESSCDInfo> GetInfoAsync()
    {
        var deviceInfo = await _zwarteDoosApiClient.GetDeviceIdAsync();
        var info = new BESSCDInfo
        {
            ExtraData = new Dictionary<string, object>
            {
                {  "DeviceId", deviceInfo.Data!.Device.Id }
            }
        };
        return info;
    }

    public async Task<EchoResponse> EchoAsync(EchoRequest request)
    {
        var response = new EchoResponse
        {
            Message = request.Message
        };
        return await Task.FromResult(response);
    }
}