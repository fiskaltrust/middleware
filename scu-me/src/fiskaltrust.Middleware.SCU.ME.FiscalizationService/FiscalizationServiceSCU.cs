using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using fiskaltrust.Middleware.SCU.ME.Common.Helpers;
using fiskaltrust.Middleware.SCU.ME.FiscalizationService.Helpers;
using Microsoft.Extensions.Logging;

using SoapFiscalizationService = FiscalizationService;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService;

#nullable enable
public sealed class FiscalizationServiceSCU : IMESSCD, IDisposable
{
    private readonly SoapFiscalizationService.FiscalizationServicePortTypeClient _fiscalizationServiceClient;
    private readonly ScuMEConfiguration _configuration;
    private readonly ILogger<FiscalizationServiceSCU> _logger;
    public FiscalizationServiceSCU(ILogger<FiscalizationServiceSCU> logger, ScuMEConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _fiscalizationServiceClient = new SoapFiscalizationService.FiscalizationServicePortTypeClient(
            SoapFiscalizationService.FiscalizationServicePortTypeClient.EndpointConfiguration.FiscalizationServicePort,
            _configuration.Sandbox
                ? "https://efitest.tax.gov.me/fs-v1/FiscalizationService.wsdl"
                : "https://efi.tax.gov.me/fs-v1/FiscalizationService.wsdl"
        );

        _fiscalizationServiceClient.Endpoint.EndpointBehaviors.Add(new FormatBehaviour());
        _fiscalizationServiceClient.Endpoint.EndpointBehaviors.Add(new SigningBehaviour(_configuration.Certificate));
    }

    public Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => Task.FromResult(new ScuMeEchoResponse { Message = request.Message });

    public async Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest)
    {
        var sendTime = registerCashDepositRequest.SubsequentDeliveryType.HasValue
            ? DateTime.Now
            : ConvertToCETFromUtc(registerCashDepositRequest.Moment);
        var request = new SoapFiscalizationService.RegisterCashDepositRequest
        {
            Header = new SoapFiscalizationService.RegisterCashDepositRequestHeaderType
            {
                SendDateTime = sendTime,
                UUID = registerCashDepositRequest.RequestId.ToString(),
            },
            CashDeposit = new SoapFiscalizationService.CashDepositType
            {
                CashAmt = registerCashDepositRequest.Amount + 0.00m,
                ChangeDateTime = sendTime,
                IssuerTIN = _configuration.TIN,
                Operation = SoapFiscalizationService.CashDepositOperationSType.INITIAL,
                TCRCode = registerCashDepositRequest.TcrCode
            }
        };

        try
        {
            var response = await _fiscalizationServiceClient.registerCashDepositAsync(request);

            return new RegisterCashDepositResponse { FCDC = response.RegisterCashDepositResponse.FCDC };
        }
        catch (Exception e)
        {
            IsConnectionException(e);
            _logger.LogError(e, "Error sending request");
            throw;
        }
    }

    public async Task RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashWithdrawalRequest)
    {
        var sendDateTime = registerCashWithdrawalRequest.SubsequentDeliveryType.HasValue ? DateTime.Now : ConvertToCETFromUtc(registerCashWithdrawalRequest.Moment);
        var request = new SoapFiscalizationService.RegisterCashDepositRequest
        {
            Header = new SoapFiscalizationService.RegisterCashDepositRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerCashWithdrawalRequest.RequestId.ToString()
            },
            CashDeposit = new SoapFiscalizationService.CashDepositType
            {
                CashAmt = registerCashWithdrawalRequest.Amount + 0.00m,
                ChangeDateTime = sendDateTime,
                IssuerTIN = _configuration.TIN,
                Operation = SoapFiscalizationService.CashDepositOperationSType.WITHDRAW,
                TCRCode = registerCashWithdrawalRequest.TcrCode
            }
        };

        try
        {
            _ = await _fiscalizationServiceClient.registerCashDepositAsync(request);
        }
        catch (Exception e)
        {
            IsConnectionException(e);
            _logger.LogError(e, "Error sending request");
            throw;
        }
    }

    public async Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
    {
        try
        {
            var sendDateTime = registerInvoiceRequest.SubsequentDeliveryType.HasValue ? DateTime.Now : ConvertToCETFromUtc(registerInvoiceRequest.Moment);

            var invoice = new SoapFiscalizationService.InvoiceType
            {
                InvType = (SoapFiscalizationService.InvoiceTSType) Enum.Parse(typeof(SoapFiscalizationService.InvoiceTSType), registerInvoiceRequest.InvoiceDetails.InvoicingType.ToString().ToUpper()),
                BankAccNum = null,
                BusinUnitCode = registerInvoiceRequest.BusinessUnitCode,
                GoodsExAmtSpecified = registerInvoiceRequest.InvoiceDetails.ExportedGoodsAmount is not null,
                IICRefs = registerInvoiceRequest.InvoiceDetails.IicReferences?.Select(r => new SoapFiscalizationService.IICRefType
                {
                    IIC = r.Iic,
                    Amount = r.Amount,
                    IssueDateTime = r.IssueDateTime
                }).ToArray(),
                IICSignature = registerInvoiceRequest.IICSignature,
                IIC = registerInvoiceRequest.IIC,
                InvNum = string.Join("/", registerInvoiceRequest.BusinessUnitCode, registerInvoiceRequest.InvoiceDetails.YearlyOrdinalNumber, sendDateTime.Year, registerInvoiceRequest.TcrCode),
                InvOrdNum = (int) registerInvoiceRequest.InvoiceDetails.YearlyOrdinalNumber,
                IsIssuerInVAT = registerInvoiceRequest.IsIssuerInVATSystem,
                IsReverseChargeSpecified = default,
                IssueDateTime = ConvertToCETFromUtc(registerInvoiceRequest.Moment),
                Items = registerInvoiceRequest.InvoiceDetails.ItemDetails?.Select(i =>
                {
                    var invoiceItem = new SoapFiscalizationService.InvoiceItemType
                    {
                        C = i.Code,
                        EXSpecified = i.ExemptFromVatReason.HasValue,
                        INSpecified = i.IsInvestment.HasValue,
                        N = i.Name,
                        PA = i.GrossAmount,
                        PB = i.NetAmount,
                        Q = (double) i.Quantity,
                        R = i.DiscountPercentage ?? 0,
                        RSpecified = i.DiscountPercentage.HasValue,
                        RR = i.IsDiscountReducingBasePrice ?? false,
                        RRSpecified = i.IsDiscountReducingBasePrice.HasValue,
                        U = i.Unit,
                        UPA = i.GrossUnitPrice,
                        UPB = i.NetUnitPrice,
                        VASpecified = i.VatAmount.HasValue,
                        VRSpecified = i.VatRate.HasValue,
                    };

                    if (i.Vouchers != null && i.Vouchers.Any())
                    {
                        var voucher = i.Vouchers.FirstOrDefault();
                        if (voucher != null)
                        {
                            invoiceItem.VD = voucher.ExpirationDate;
                            invoiceItem.VDSpecified = true;
                        }
                        invoiceItem.VSN = string.Join(Environment.NewLine, i.Vouchers.Select(i => i.SerialNumbers));
                    }

                    if (i.ExemptFromVatReason.HasValue)
                    {
                        invoiceItem.EX = (SoapFiscalizationService.ExemptFromVATSType) i.ExemptFromVatReason.Value;
                    }

                    if (i.IsInvestment.HasValue)
                    {
                        invoiceItem.IN = i.IsInvestment.Value;
                    }

                    if (i.VatAmount.HasValue)
                    {
                        invoiceItem.VA = i.VatAmount.Value;
                    }

                    if (i.VatRate.HasValue)
                    {
                        invoiceItem.VR = i.VatRate.Value;
                    }
                    return invoiceItem;
                }).ToArray(),
                MarkUpAmt = default,
                MarkUpAmtSpecified = default,
                Note = null,
                OperatorCode = registerInvoiceRequest.OperatorCode,
                ParagonBlockNum = null,
                PayDeadlineSpecified = registerInvoiceRequest.InvoiceDetails.PaymentDeadline is not null,
                PayMethods = registerInvoiceRequest.InvoiceDetails.PaymentDetails?.Select(p => new SoapFiscalizationService.PayMethodType
                {
                    AdvIIC = null,
                    Amt = p.Amount,
                    BankAcc = null,
                    CompCard = p.CompanyCardNumber,
                    Type = (SoapFiscalizationService.PaymentMethodTypeSType) Enum.Parse(typeof(SoapFiscalizationService.PaymentMethodTypeSType), p.Type.ToString().ToUpper())
                    //Vouchers = p.VoucherNumbers?.Select(v => new SoapFiscalizationService.VoucherType { Num = v })?.ToArray() ?? new SoapFiscalizationService.VoucherType[0]
                }).ToArray(),
                SameTaxes = registerInvoiceRequest.InvoiceDetails.ItemDetails?.Where(x => x.VatRate.HasValue).GroupBy(g => g.VatRate).Select(s => new SoapFiscalizationService.SameTaxType
                {
                    VATRate = s.First().VatRate ?? 0,
                    VATRateSpecified = true,
                    NumOfItems = (int) s.Sum(x => Math.Abs(x.Quantity)),
                    ExemptFromVATSpecified = false,
                    PriceBefVAT = s.Sum(x => x.NetUnitPrice),
                    VATAmtSpecified = false
                }).ToArray(),
                SoftCode = registerInvoiceRequest.SoftwareCode,
                Seller = new SoapFiscalizationService.SellerType {
                                IDType = SoapFiscalizationService.IDTypeSType.TIN,
                                IDNum = _configuration.TIN,
                                Name = _configuration.PosOperatorName,
                                Address = _configuration.PosOperatorAddress,
                                Town = _configuration.PosOperatorTown,
                                CountrySpecified = false
                },
            TaxFreeAmtSpecified = registerInvoiceRequest.InvoiceDetails.TaxFreeAmount is > 0,
            TaxPeriod = default, //registerInvoiceRequest.InvoiceDetails.TaxPeriod,
            TCRCode = registerInvoiceRequest.TcrCode,
            TotPrice = registerInvoiceRequest.InvoiceDetails.GrossAmount,
            TotPriceToPay = default,
            TotPriceToPaySpecified = default,
            TotPriceWoVAT = registerInvoiceRequest.InvoiceDetails.NetAmount,
            TotVATAmtSpecified = registerInvoiceRequest.InvoiceDetails.TotalVatAmount.HasValue,
            TypeOfInv = (SoapFiscalizationService.InvoiceSType) Enum.Parse(typeof(SoapFiscalizationService.InvoiceSType), registerInvoiceRequest.InvoiceDetails.InvoiceType.ToString().ToUpper()),
            TypeOfSelfIssSpecified = registerInvoiceRequest.InvoiceDetails.SelfIssuedInvoiceType.HasValue,
        };

        if (registerInvoiceRequest.InvoiceDetails.Buyer is not null)
        {
            invoice.Buyer = new SoapFiscalizationService.BuyerType
            {
                Address = registerInvoiceRequest.InvoiceDetails.Buyer.Address,
                CountrySpecified = registerInvoiceRequest.InvoiceDetails.Buyer.Country is not null,
                IDType = (SoapFiscalizationService.IDTypeSType) registerInvoiceRequest.InvoiceDetails.Buyer.IdentificationType,
                IDNum = registerInvoiceRequest.InvoiceDetails.Buyer.IdentificationNumber,
                Name = registerInvoiceRequest.InvoiceDetails.Buyer.Name,
                Town = registerInvoiceRequest.InvoiceDetails.Buyer.Town
            };

            if (registerInvoiceRequest.InvoiceDetails.Buyer.Country is not null)
            {
                invoice.Buyer.Country = (SoapFiscalizationService.CountryCodeSType) Enum.Parse(typeof(SoapFiscalizationService.CountryCodeSType), registerInvoiceRequest.InvoiceDetails.Buyer.Country);
            }
        }

        if (registerInvoiceRequest.InvoiceDetails.InvoiceCorrectionDetails is not null)
        {
            invoice.CorrectiveInv = new SoapFiscalizationService.CorrectiveInvType
            {
                IICRef = registerInvoiceRequest.InvoiceDetails.InvoiceCorrectionDetails!.ReferencedIKOF,
                IssueDateTime = registerInvoiceRequest.InvoiceDetails.InvoiceCorrectionDetails.ReferencedMoment,
                Type = SoapFiscalizationService.CorrectiveInvTypeSType.CORRECTIVE
            };
        }

        if (registerInvoiceRequest.InvoiceDetails.Currency is not null)
        {
            invoice.Currency = new SoapFiscalizationService.CurrencyType
            {
                Code = (SoapFiscalizationService.CurrencyCodeSType) Enum.Parse(typeof(SoapFiscalizationService.CurrencyCodeSType), registerInvoiceRequest.InvoiceDetails.Currency.CurrencyCode),
                ExRate = Convert.ToDouble(registerInvoiceRequest.InvoiceDetails.Currency.ExchangeRateToEuro)
            };
        }

        if (registerInvoiceRequest.InvoiceDetails.ExportedGoodsAmount.HasValue)
        {
            invoice.GoodsExAmt = registerInvoiceRequest.InvoiceDetails.ExportedGoodsAmount.Value;
        }

        if (registerInvoiceRequest.InvoiceDetails.PaymentDeadline.HasValue)
        {
            invoice.PayDeadline = registerInvoiceRequest.InvoiceDetails.PaymentDeadline.Value;
        }

        if (registerInvoiceRequest.InvoiceDetails.TaxFreeAmount.HasValue)
        {
            invoice.TaxFreeAmt = registerInvoiceRequest.InvoiceDetails.TaxFreeAmount.Value;
        }

        if (registerInvoiceRequest.InvoiceDetails.TotalVatAmount.HasValue)
        {
            invoice.TotVATAmt = registerInvoiceRequest.InvoiceDetails.TotalVatAmount.Value;
        }

        if (registerInvoiceRequest.InvoiceDetails.SelfIssuedInvoiceType.HasValue)
        {
            invoice.TypeOfSelfIss = (SoapFiscalizationService.SelfIssSType) registerInvoiceRequest.InvoiceDetails.SelfIssuedInvoiceType.Value;
        }

        var request = new SoapFiscalizationService.RegisterInvoiceRequest
        {
            Header = new SoapFiscalizationService.RegisterInvoiceRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerInvoiceRequest.RequestId.ToString()
            },
            Invoice = invoice
        };

            var response = await _fiscalizationServiceClient.registerInvoiceAsync(request);
            return new RegisterInvoiceResponse
            {
                FIC = response.RegisterInvoiceResponse.FIC,
            };
        }
        catch (Exception e)
        {
            IsConnectionException(e);
            _logger.LogError(e, "Error sending request");
            throw;
        }
    }


    public async Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTCRRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterTCRRequest
        {
            Header = new SoapFiscalizationService.RegisterTCRRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerTCRRequest.RequestId.ToString()
            },
            TCR = new SoapFiscalizationService.TCRType
            {
                BusinUnitCode = registerTCRRequest.BusinessUnitCode,
                IssuerTIN = _configuration.TIN,
                MaintainerCode = registerTCRRequest.TcrSoftwareMaintainerCode,
                SoftCode = registerTCRRequest.TcrSoftwareCode,
                TCRIntID = registerTCRRequest.InternalTcrIdentifier,
                TypeSpecified = registerTCRRequest.TcrType is not null,
                ValidFrom = sendDateTime,
                ValidFromSpecified = true,
                ValidToSpecified = false
            },
        };

        if (registerTCRRequest.TcrType is not null)
        {
            request.TCR.Type = (SoapFiscalizationService.TCRSType) registerTCRRequest.TcrType;
        }

        try
        {
            var response = await _fiscalizationServiceClient.registerTCRAsync(request);
            _logger.LogInformation("Client registered!");
            return new RegisterTcrResponse
            {
                TcrCode = response.RegisterTCRResponse.TCRCode,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending request");
            throw;
        }
    }

    public async Task UnregisterTcrAsync(UnregisterTcrRequest unregisterTCRRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterTCRRequest
        {
            Header = new SoapFiscalizationService.RegisterTCRRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = unregisterTCRRequest.RequestId.ToString()
            },
            TCR = new SoapFiscalizationService.TCRType
            {
                BusinUnitCode = unregisterTCRRequest.BusinessUnitCode,
                IssuerTIN = _configuration.TIN,
                MaintainerCode = unregisterTCRRequest.TcrSoftwareMaintainerCode,
                SoftCode = unregisterTCRRequest.TcrSoftwareCode,
                TCRIntID = unregisterTCRRequest.InternalTcrIdentifier,
                TypeSpecified = unregisterTCRRequest.TcrType is not null,
                ValidFromSpecified = false,
                ValidTo = sendDateTime,
                ValidToSpecified = true
            },
        };

        if (unregisterTCRRequest.TcrType is not null)
        {
            request.TCR.Type = (SoapFiscalizationService.TCRSType) unregisterTCRRequest.TcrType;
        }
        try
        {
            _ = await _fiscalizationServiceClient.registerTCRAsync(request);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending request");
            throw;
        }
    }

    public void Dispose()
    {
        ((IDisposable) _fiscalizationServiceClient).Dispose();
    }

    private DateTime ConvertToCETFromUtc(DateTime dateTime)
    {
        DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, cstZone);
    }

    public Task<ComputeIICResponse> ComputeIICAsync(ComputeIICRequest computeIICRequest)
    {
        var (iic, iicSignature) = SigningHelper.CreateIIC(_configuration, computeIICRequest);
        return Task.FromResult(
        new ComputeIICResponse
        {
            IIC = iic,
            IICSignature = iicSignature
        });
    }

    private void IsConnectionException(Exception e)
    {
        if (e is EndpointNotFoundException or WebException or CommunicationException)
        {
            _logger.LogError(e, "Error sending request");
            throw new FiscalizationException("No access to Fiscalization Endpoint!", e);
        }
        _logger.LogError(e, "Error sending request");
        throw e;
    }
}
