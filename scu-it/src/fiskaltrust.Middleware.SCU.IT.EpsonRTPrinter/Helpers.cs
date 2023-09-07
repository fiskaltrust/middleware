using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.QueueLogic.Extensions;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class Helpers
{
    public static FiscalReceiptInvoice CreateInvoice(ReceiptRequest request)
    {
        var fiscalReceiptRequest = new FiscalReceiptInvoice()
        {
            //Barcode = ChargeItem.ProductBarcode,
            //TODO DisplayText = "Message on customer display",
            Operator = request.cbUser,
            Items = request.cbChargeItems.Where(x => !x.IsV2PaymentAdjustment()).Select(p => new Item
            {
                Description = p.Description,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice ?? p.Amount / p.Quantity,
                Amount = p.Amount,
                VatGroup = p.GetV2VatGroup(),
                AdditionalInformation = p.ftChargeItemCaseData
            }).ToList(),
            PaymentAdjustments = request.GetV2PaymentAdjustments(),
            Payments = request.GetV2Payments()
        };
        return fiscalReceiptRequest;
    }

    public static FiscalReceiptRefund CreateRefund(ReceiptRequest request, long receiptnumber, long zReceiptNumber, DateTime receiptDateTime, string serialNumber)
    {
        return new FiscalReceiptRefund()
        {
            //TODO Barcode = "0123456789" 
            Operator = "1",
            DisplayText = $"REFUND {zReceiptNumber:D4} {receiptnumber:D4} {receiptDateTime:ddMMyyyy} {serialNumber}",
            Refunds = request.cbChargeItems?.Select(p => new Refund
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = Math.Abs(p.Amount) / Math.Abs(p.Quantity),
                Amount = Math.Abs(p.Amount),
                VatGroup = p.GetV2VatGroup()
            }).ToList(),
            PaymentAdjustments = request.GetV2PaymentAdjustments(),
            Payments = request.cbPayItems?.Select(p => new Payment
            {
                Amount = Math.Abs(p.Amount),
                Description = p.Description,
                PaymentType = p.GetV2PaymentType(),
            }).ToList()
        };
    }

    public static ProcessResponse CreateResponse(ReceiptResponse receiptResponse)
    {
        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse
        };
    }

    public static bool IsNoActionCase(ReceiptRequest request)
    {
        return NonProcessingCases.Select(x => (long) x).Contains(request.GetReceiptCase());
    }


    public static readonly List<ITReceiptCases> NonProcessingCases = new List<ITReceiptCases>
        {
            ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003,
            ITReceiptCases.ECommerce0x0004,
            ITReceiptCases.InvoiceUnknown0x1000,
            ITReceiptCases.InvoiceB2C0x1001,
            ITReceiptCases.InvoiceB2B0x1002,
            ITReceiptCases.InvoiceB2G0x1003,
            ITReceiptCases.ZeroReceipt0x200,
            ITReceiptCases.OneReceipt0x2001,
            ITReceiptCases.ShiftClosing0x2010,
            ITReceiptCases.MonthlyClosing0x2012,
            ITReceiptCases.YearlyClosing0x2013,
            ITReceiptCases.ProtocolUnspecified0x3000,
            ITReceiptCases.ProtocolTechnicalEvent0x3001,
            ITReceiptCases.ProtocolAccountingEvent0x3002,
            ITReceiptCases.InternalUsageMaterialConsumption0x3003,
            ITReceiptCases.InitSCUSwitch0x4011,
            ITReceiptCases.FinishSCUSwitch0x4012,
        };

    public static FiscalReceiptResponse ExceptionInfo(Exception e)
    {
        var msg = e.Message;
        if (e.InnerException != null)
        {
            msg += " " + e.InnerException.Message;
        }
        if (IsConnectionException(e))
        {
            return new FiscalReceiptResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.Connection } };
        }
        return new FiscalReceiptResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.General } };
    }

    public static bool IsConnectionException(Exception e)
    {
        if (e.GetType().IsAssignableFrom(typeof(EndpointNotFoundException)) ||
            e.GetType().IsAssignableFrom(typeof(WebException)) ||
            e.GetType().IsAssignableFrom(typeof(CommunicationException)) ||
            e.GetType().IsAssignableFrom(typeof(TaskCanceledException)) ||
            e.GetType().IsAssignableFrom(typeof(HttpRequestException)))
        {
            return true;
        }
        return false;
    }

    public static string? GetPrinterStatus(string? printerStatus)
    {
        var pst = printerStatus?.ToCharArray();
        if (pst != null)
        {
            var printerstatus = new DeviceStatus(Array.ConvertAll(pst, c => (int) char.GetNumericValue(c)));
            return JsonConvert.SerializeObject(printerstatus);
        }

        return null;
    }

    public static string ParseStatus(string? mfStatus)
    {
        return mfStatus switch
        {
            "01" => "Not in service",
            "02" => "In service",
            _ => "Undefined"
        };
    }
}