using System;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class Helpers
{
    public static ProcessResponse CreateResponse(ReceiptResponse receiptResponse)
    {
        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse
        };
    }

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