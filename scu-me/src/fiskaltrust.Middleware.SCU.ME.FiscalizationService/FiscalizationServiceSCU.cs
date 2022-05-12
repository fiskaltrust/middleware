using System;
using System.Linq;
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

        _fiscalizationServiceClient.Endpoint.EndpointBehaviors.Add(new DateTimeBehaviour());
        _fiscalizationServiceClient.Endpoint.EndpointBehaviors.Add(new SigningBehaviour(_configuration.Certificate));
    }

    public Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => Task.FromResult(new ScuMeEchoResponse { Message = request.Message });

    public async Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterCashDepositRequest
        {
            Header = new SoapFiscalizationService.RegisterCashDepositRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerCashDepositRequest.RequestId.ToString(),
            },
            CashDeposit = new SoapFiscalizationService.CashDepositType
            {
                CashAmt = registerCashDepositRequest.Amount,
                ChangeDateTime = registerCashDepositRequest.Moment,
                IssuerTIN = _configuration.TIN,
                Operation = SoapFiscalizationService.CashDepositOperationSType.INITIAL,
                TCRCode = registerCashDepositRequest.TcrCode
            }
        };

        var response = await _fiscalizationServiceClient.registerCashDepositAsync(request);

        return new RegisterCashDepositResponse
        {
            FCDC = response.RegisterCashDepositResponse.FCDC
        };
    }

    public async Task<RegisterCashWithdrawalResponse> RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest)
    {
        var sendDateTime = DateTime.Now;
        var request = new SoapFiscalizationService.RegisterCashDepositRequest
        {
            Header = new SoapFiscalizationService.RegisterCashDepositRequestHeaderType
            {
                SendDateTime = sendDateTime,
                UUID = registerCashDepositRequest.RequestId.ToString()
            },
            CashDeposit = new SoapFiscalizationService.CashDepositType
            {
                CashAmt = registerCashDepositRequest.Amount,
                ChangeDateTime = registerCashDepositRequest.Moment,
                IssuerTIN = _configuration.TIN,
                Operation = SoapFiscalizationService.CashDepositOperationSType.WITHDRAW,
                TCRCode = registerCashDepositRequest.TcrCode
            }
        };

        _ = await _fiscalizationServiceClient.registerCashDepositAsync(request);

        return new RegisterCashWithdrawalResponse { };
    }

    public async Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest)
    {
        var sendDateTime = DateTime.Now;

        var iic = SigningHelper.CreateIIC(_configuration, registerInvoiceRequest);

        var invoice = new SoapFiscalizationService.InvoiceType
        {
            Approvals = null,
            BadDebtInv = null,
            BankAccNum = null,
            BusinUnitCode = registerInvoiceRequest.BusinessUnitCode,
            Fees = null,
            GoodsExAmtSpecified = registerInvoiceRequest.InvoiceDetails.ExportedGoodsAmount is not null,
            IICRefs = null,
            IICSignature = null,
            IIC = iic,
            InvNum = registerInvoiceRequest.RequestId.ToString(),
            InvOrdNum = (int) registerInvoiceRequest.InvoiceDetails.YearlyOrdinalNumber,
            InvType = (SoapFiscalizationService.InvoiceTSType) registerInvoiceRequest.InvoiceDetails.InvoiceType,
            IsIssuerInVAT = registerInvoiceRequest.IsIssuerInVATSystem,
            IsReverseChargeSpecified = null,
            IssueDateTime = registerInvoiceRequest.Moment,
            Items = registerInvoiceRequest.InvoiceDetails.ItemDetails.Select(i =>
            {
                var invoiceItem = new SoapFiscalizationService.InvoiceItemType
                {
                    C = i.Code,
                    EXSpecified = i.ExemptFromVatReason.HasValue,
                    INSpecified = i.IsInvestment.HasValue,
                    N = i.Name,
                    PA = null,
                    PB = null,
                    Q = (double) i.Quantity,
                    R = null,
                    RSpecified = null,
                    RR = null,
                    RRSpecified = null,
                    U = i.Unit,
                    UPA = null,
                    UPB = null,
                    VASpecified = i.VatAmount.HasValue,
                    VD = null,
                    VDSpecified = null,
                    VRSpecified = i.VatRate.HasValue,
                    VSN = null
                };

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
            MarkUpAmt = null,
            MarkUpAmtSpecified = null,
            Note = null,
            OperatorCode = registerInvoiceRequest.OperatorCode,
            ParagonBlockNum = null,
            PayDeadlineSpecified = registerInvoiceRequest.InvoiceDetails.PaymentDeadline is not null,
            PayMethods = registerInvoiceRequest.InvoiceDetails.PaymentDetails.Select(p => new SoapFiscalizationService.PayMethodType
            {
                AdvIIC = null,
                Amt = p.Amount,
                BankAcc = null,
                CompCard = p.CompanyCardNumber,
                Type = (SoapFiscalizationService.PaymentMethodTypeSType) p.Type,
                Vouchers = p.VoucherNumbers.Select(v => new SoapFiscalizationService.VoucherType { Num = v }).ToArray()
            }).ToArray(),
            SameTaxes = null,
            SoftCode = registerInvoiceRequest.SoftwareCode,
            Seller = null,
            SupplyDateOrPeriod = null,
            TaxFreeAmtSpecified = registerInvoiceRequest.InvoiceDetails.TaxFreeAmount.HasValue,
            TaxPeriod = registerInvoiceRequest.InvoiceDetails.TaxPeriod,
            TCRCode = registerInvoiceRequest.TcrCode,
            TotPrice = registerInvoiceRequest.InvoiceDetails.GrossAmount,
            TotPriceToPay = null,
            TotPriceToPaySpecified = null,
            TotPriceWoVAT = registerInvoiceRequest.InvoiceDetails.NetAmount,
            TotVATAmtSpecified = registerInvoiceRequest.InvoiceDetails.TotalVatAmount.HasValue,
            TypeOfInv = (SoapFiscalizationService.InvoiceSType) registerInvoiceRequest.InvoiceDetails.InvoiceType,
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
            IIC = iic
        };
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


        var response = await _fiscalizationServiceClient.registerTCRAsync(request);

        return new RegisterTcrResponse
        {
            TcrCode = response.RegisterTCRResponse.TCRCode,
        };
    }

    public async Task UnregisterTcrAsync(RegisterTcrRequest registerTCRRequest)
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
                ValidFromSpecified = false,
                ValidTo = sendDateTime,
                ValidToSpecified = true
            },
        };

        if (registerTCRRequest.TcrType is not null)
        {
            request.TCR.Type = (SoapFiscalizationService.TCRSType) registerTCRRequest.TcrType;
        }

        _ = await _fiscalizationServiceClient.registerTCRAsync(request);

        return;
    }

    public void Dispose()
    {
        ((IDisposable) _fiscalizationServiceClient).Dispose();
    }
}