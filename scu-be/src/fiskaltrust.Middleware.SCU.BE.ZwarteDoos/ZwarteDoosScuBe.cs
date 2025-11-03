using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Helpers;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
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
            var signSalesRequest = new SaleInput
            {
                BookingDate = DateOnly.FromDateTime(receiptRequest.cbReceiptMoment),
                BookingPeriodId = receiptResponse.ftQueueItemID,
                DeviceId = _configuration.DeviceId,
                EmployeeId = receiptRequest.cbUser is string userString ? userString : "undefined",
                EstNo = "",
                Language = Models.Enums.Language.NL,
                PosDateTime = receiptRequest.cbReceiptMoment,
                PosFiscalTicketNo = receiptResponse.ftQueueRow,
                PosId = receiptResponse.ftCashBoxIdentification,
                PosSwVersion = "1.3.0",
                TerminalId = receiptResponse.cbTerminalID ?? "undefined",
                TicketMedium = Models.Enums.TicketMedium.PAPER,
                VatNo = "",
                Financials = [],
                CostCenter = null,
                FdmRef = null,
                Transaction = null
            };
            var apiResponse = await _zwarteDoosApiClient.SaleAsync(signSalesRequest, isTraining: receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training));


            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var dailyClosingRequest = new ReportTurnoverZInput
            {
                BookingDate = DateOnly.FromDateTime(receiptRequest.cbReceiptMoment),
                BookingPeriodId = receiptResponse.ftQueueItemID,
                DeviceId = _configuration.DeviceId,
                EmployeeId = receiptRequest.cbUser is string userString ? userString : "undefined",
                EstNo = "",
                FdmDevices = [],
                Language = Models.Enums.Language.NL,
                PosDateTime = receiptRequest.cbReceiptMoment,
                PosDevices = [],
                PosFiscalTicketNo = receiptResponse.ftQueueRow,
                PosId = receiptResponse.ftCashBoxIdentification,
                PosSwVersion = "1.3.0",
                ReportBookingDate = DateOnly.FromDateTime(receiptRequest.cbReceiptMoment),
                ReportNo = receiptResponse.ftQueueRow,
                TerminalId = receiptResponse.cbTerminalID ?? "undefined",
                TicketMedium = Models.Enums.TicketMedium.PAPER,
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
                },
                VatNo = ""
            };

            var apiResponse = await _zwarteDoosApiClient.ReportTurnoverZAsync(dailyClosingRequest, isTraining: receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training));
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
            InfoData = JsonSerializer.Serialize(deviceInfo)
        };
        return info;
    }
}