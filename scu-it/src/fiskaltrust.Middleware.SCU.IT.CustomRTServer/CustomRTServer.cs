using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

#pragma warning disable

#nullable enable
public sealed partial class CustomRTServer : IITSSCD
{
    private readonly ILogger<CustomRTServer> _logger;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

    public CustomRTServer(ILogger<CustomRTServer> logger, CustomRTServerConfiguration configuration)
    {
        _logger = logger;
        if (string.IsNullOrEmpty(configuration.ServerUrl))
        {
            throw new NullReferenceException("ServerUrl is not set.");
        }
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration.ServerUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
    }

    /** These methods are kept for backwards compatibility with the interface but we will not use them **/

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
    public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotImplementedException();
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => throw new NotImplementedException();

    public async Task<RTInfo> GetRTInfoAsync() 
    {
        return new RTInfo 
        {
            SerialNumber = null,
            InfoData = null
        };
    }
    
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCaseVersion = request.ReceiptRequest.ftReceiptCase & 0xF000;
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFF;

        if (receiptCaseVersion == 0x0000)
        {
            receiptCase = GetMappedReceitCase(receiptCase);
        }


        switch (receiptCase)
        {
            case (long) ITReceiptCases.ZeroReceipt0x200:
                break;
            case (long) ITReceiptCases.DailyClosing0x212:
                break;
            case (long) ITReceiptCases.ShiftClosing0x211: 
            case (long) ITReceiptCases.MonthlyClosing0x213: 
            case (long) ITReceiptCases.YearlyClosing0x214: 
                break;

            case (long) ITReceiptCases.InitialOperationReceipt0xF01:
                break;
            case (long) ITReceiptCases.OutOfOperationReceipt0xF02:
                break;

            case (long) ITReceiptCases.InvoiceUnpsecified0x101:
                break;
            case (long) ITReceiptCases.InvoiceB2B0x102:
                break;
            case (long) ITReceiptCases.InvoiceB2C0x103:
                break;
            case (long) ITReceiptCases.InvoiceB2G0x104:
                break;
            
            case (long) ITReceiptCases.ProtocolTechnicalEvent0x301:
            case (long) ITReceiptCases.ProtocolAccountingEvent0x302:
            case (long) ITReceiptCases.ProtoclUnspecified0x303:
            case (long) ITReceiptCases.InternalUsageMaterialConsumption0x304:
                break;

            case (long) ITReceiptCases.CashDepositPayInn0x002:
            case (long) ITReceiptCases.CashPayOut0x003:
            case (long) ITReceiptCases.PaymentTransfer0x004:
            case (long) ITReceiptCases.POSReceiptWithoutCashRegisterObligation0x005:
            case (long) ITReceiptCases.ECommerce0x006:
            case (long) ITReceiptCases.SaleInForeignCountry0x007:
            case (long) ITReceiptCases.UnknownReceipt0x00:
            case (long) ITReceiptCases.POSReceipt0x001: 
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    private long GetMappedReceitCase(long legacyReceiptcase)
    {
        var value = legacyReceiptcase switch
        {
            0x002 => ITReceiptCases.ZeroReceipt0x200,
            0x003 => ITReceiptCases.InitialOperationReceipt0xF01,
            0x004 => ITReceiptCases.OutOfOperationReceipt0xF02,
            0x005 => ITReceiptCases.MonthlyClosing0x213,
            0x006 => ITReceiptCases.YearlyClosing0x214,
            0x007 => ITReceiptCases.DailyClosing0x212,
            0x000 => ITReceiptCases.UnknownReceipt0x00,
            0x001 => ITReceiptCases.POSReceipt0x001,
            _ => ITReceiptCases.UnknownReceipt0x00
        };
        return (long) value;
    }
}
