using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Helpers;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosScuBe : IBESSCD
{
    private readonly ZwarteDoosScuConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ZwarteDoosFactory _zwarteDoosFactory;
    private readonly ILogger<ZwarteDoosScuBe> _logger;

    public ZwarteDoosScuBe(ILogger<ZwarteDoosScuBe> logger, ILoggerFactory loggerFactory, ZwarteDoosScuConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler);
        _zwarteDoosFactory = new ZwarteDoosFactory(
            loggerFactory.CreateLogger<ZwarteDoosFactory>(),
            _httpClient,
            _configuration);
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
                return ProcessResponseHelpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
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
            await Task.CompletedTask;
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }


    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptResponse receiptResponse)
    {
        try
        {
            await Task.CompletedTask;
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }


    public Task<BESSCDInfo> GetInfoAsync()
    {
        _logger.LogInformation("Getting ZwarteDoos SCU info");

        var info = new BESSCDInfo();
        return Task.FromResult(info);
    }
}