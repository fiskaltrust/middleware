using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using System.Linq;
using fiskaltrust.ifPOS.v1.errors;
using System.Globalization;
using fiskaltrust.Middleware.SCU.IT.Epson.QueueLogic.Extensions;
using fiskaltrust.Middleware.SCU.IT.Epson;
using fiskaltrust.Middleware.SCU.IT.Abstraction;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class PosReceiptCommand 
    {
        private readonly long _countryBaseState;
        private readonly EpsonSCU _epsonSCU;

        public PosReceiptCommand(EpsonSCU epsonSCU)
        {
            _epsonSCU = epsonSCU;
        }

        public async Task<ReceiptResponse> ExecuteAsync(ReceiptRequest request, ReceiptResponse receiptResponse)
        {
            if (request.IsMultiUseVoucherSale())
            {
                return await CreateNonFiscalRequestAsync(receiptResponse, request).ConfigureAwait(false);
            }

            FiscalReceiptResponse fiscalResponse;
            if (request.IsVoid())
            {
                // TODO how will we get the refund information? ==> signatures??
                var fiscalReceiptRefund = await CreateRefundAsync(request, -1, -1, DateTime.MinValue).ConfigureAwait(false);
                fiscalResponse = await _epsonSCU.FiscalReceiptRefundAsync(fiscalReceiptRefund).ConfigureAwait(false);
            }
            else
            {
                var fiscalReceiptinvoice = CreateInvoice(request);
                fiscalResponse = await _epsonSCU.FiscalReceiptInvoiceAsync(fiscalReceiptinvoice).ConfigureAwait(false);
            }
            if (!fiscalResponse.Success)
            {
                throw new SSCDErrorException(fiscalResponse.SSCDErrorInfo.Type, fiscalResponse.SSCDErrorInfo.Info);
            }
            else
            {
                receiptResponse.ftReceiptIdentification += $"{fiscalResponse.ReceiptNumber}";
                receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalResponse.ReceiptNumber, fiscalResponse.ZRepNumber, fiscalResponse.Amount, fiscalResponse.ReceiptDateTime);
            }
            return receiptResponse;
        }

        private async Task<ReceiptResponse> CreateNonFiscalRequestAsync(ReceiptResponse receiptResponse, ReceiptRequest request)
        {
            var nonFiscalRequest = new NonFiscalRequest
            {
                NonFiscalPrints = new List<NonFiscalPrint>()
            };
            if (request.cbChargeItems != null)
            {
                foreach (var chargeItem in request.cbChargeItems.Where(x => x.IsMultiUseVoucherSale()))
                {
                    AddVoucherNonFiscalPrints(nonFiscalRequest.NonFiscalPrints, chargeItem.Amount, chargeItem.ftChargeItemCaseData);
                }
            }
            if (request.cbPayItems != null)
            {
                foreach (var payItem in request.cbPayItems.Where(x => x.IsVoucherSale()))
                {
                    AddVoucherNonFiscalPrints(nonFiscalRequest.NonFiscalPrints, payItem.Amount, payItem.ftPayItemCaseData);
                }
            }
            var response = await _epsonSCU.NonFiscalReceiptAsync(nonFiscalRequest);
            if (response.Success)
            {
                receiptResponse.ftSignatures = SignatureFactory.CreateVoucherSignatures(nonFiscalRequest);
            }
            return receiptResponse;
        }

        private static void AddVoucherNonFiscalPrints(List<NonFiscalPrint> nonFiscalPrints, decimal amount, string info)
        {
            nonFiscalPrints.Add(new NonFiscalPrint() { Data = "***Voucher***", Font = 2 });
            if (!string.IsNullOrEmpty(info))
            {
                nonFiscalPrints.Add(new NonFiscalPrint() { Data = info, Font = 2 });
            }
            nonFiscalPrints.Add(new NonFiscalPrint()
            {
                Data = Math.Abs(amount).ToString(new NumberFormatInfo
                {
                    NumberDecimalSeparator = ",",
                    NumberGroupSeparator = "",
                    CurrencyDecimalDigits = 2
                }),
                Font = 2
            });
        }

        private static FiscalReceiptInvoice CreateInvoice(ReceiptRequest request)
        {
            var fiscalReceiptRequest = new FiscalReceiptInvoice()
            {
                //Barcode = ChargeItem.ProductBarcode,
                //TODO DisplayText = "Message on customer display",
                Operator = request.cbUser,
                Items = request.cbChargeItems.Where(x => !x.IsPaymentAdjustment()).Select(p => new Item
                {
                    Description = p.Description,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice ?? p.Amount / p.Quantity,
                    Amount = p.Amount,
                    VatGroup = p.GetVatGroup(),
                    AdditionalInformation = p.ftChargeItemCaseData
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.GetPayments()
            };
            return fiscalReceiptRequest;
        }

        private async Task<FiscalReceiptRefund> CreateRefundAsync(ReceiptRequest request, long receiptnumber, long zReceiptNumber, DateTime receiptDateTime)
        {
            var deviceInfo = await _epsonSCU.GetDeviceInfoAsync();
            var fiscalReceiptRequest = new FiscalReceiptRefund()
            {
                //TODO Barcode = "0123456789" 
                Operator = "1",
                DisplayText = $"REFUND {zReceiptNumber:D4} {receiptnumber:D4} {receiptDateTime:ddMMyyyy} {deviceInfo.SerialNumber}",
                Refunds = request.cbChargeItems?.Select(p => new Refund
                {
                    Description = p.Description,
                    Quantity = Math.Abs(p.Quantity),
                    UnitPrice = p.UnitPrice ?? 0,
                    Amount = Math.Abs(p.Amount),
                    VatGroup = p.GetVatGroup()
                }).ToList(),
                PaymentAdjustments = request.GetPaymentAdjustments(),
                Payments = request.cbPayItems?.Select(p => new Payment
                {
                    Amount = p.Amount,
                    Description = p.Description,
                    PaymentType = p.GetPaymentType(),
                }).ToList()
            };
            return fiscalReceiptRequest;
        }
    }
}
