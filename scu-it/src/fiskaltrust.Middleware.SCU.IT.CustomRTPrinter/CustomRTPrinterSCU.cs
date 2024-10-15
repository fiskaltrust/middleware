using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
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
    private string _serialnr;

    public CustomRTPrinterSCU(ILogger<CustomRTPrinterSCU> logger, CustomRTPrinterConfiguration configuration)
    {
        _logger = logger;
        _printerClient = new CustomRTPrinterClient(configuration);
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });
    public override async Task<RTInfo> GetRTInfoAsync()
    {
        var info = await _printerClient.SendCommand<InfoResp>(new GetInfo());

        _serialnr = info.SerialNumber;
        return new RTInfo
        {
            InfoData = JsonConvert.SerializeObject(info), // TODO: for gods sake don't use newtonsoft
                                                          // TODO: Do we need to map some properties or can the InfoData be anything?
            SerialNumber = info.SerialNumber
        };
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            var receiptCase = request.ReceiptRequest.GetReceiptCase();
            if (string.IsNullOrEmpty(_serialnr))
            {
                var info = await _printerClient.SendCommand<InfoResp>(new GetInfo());
                _logger.LogInformation(JsonConvert.SerializeObject(info));
                _serialnr = info.SerialNumber;
            }
            if (request.ReceiptRequest.IsInitialOperationReceipt())
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateInitialOperationSignatures().ToList());
            }

            if (request.ReceiptRequest.IsOutOfOperationReceipt())
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateOutOfOperationSignatures().ToList());
            }

            //if (request.ReceiptRequest.IsZeroReceipt())
            //{
            //    (var signatures, var stateData) = await PerformZeroReceiptOperationAsync();
            //    return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, stateData, signatures);
            //}

            //if (request.ReceiptRequest.IsVoid())
            //{
            //    return await ProcessVoidReceipt(request);
            //}

            //if (request.ReceiptRequest.IsRefund())
            //{
            //    return await ProcessRefundReceipt(request);
            //}

            if (request.ReceiptRequest.IsDailyClosing())
            {
                return CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }

            //if (request.ReceiptRequest.IsReprint())
            //{
            //    return await ProcessPerformReprint(request);
            //}

            //if (receiptCase == (long) ITReceiptCases.Protocol0x0005)
            //{
            //    return CreateResponse(await PerformProtocolReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            //}

            switch (receiptCase)
            {
                case (long) ITReceiptCases.UnknownReceipt0x0000:
                case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
                    return CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            }
            request.ReceiptResponse.SetReceiptResponseErrored($"The given receiptcase 0x{receiptCase.ToString("X")} is not supported by Custom RT Printer.");
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
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
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
            var response = await _printerClient.SendFiscalReport<ZReportsResp>(new PrintZReport());
            // TODO read daily closing number
            receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(-1);
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
   

            var fiscalRecordes = receiptRequest.cbChargeItems.Select(c => new PrintRecItem
            {
                Description = c.Description,
                Quantity = Math.Abs(c.Quantity),
                UnitPrice = c.Quantity == 0 || c.Amount == 0 ? 0 : Math.Abs(c.Amount) / Math.Abs(c.Quantity),
                Department = 1,
                IdVat = 1
            }).ToList();

            var response = await _printerClient.SendFiscalReceipt<Response<InfoResp>>(fiscalRecordes.ToArray());

            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = int.Parse(response.AddInfo.NClose),
                RTDocNumber = int.Parse(response.AddInfo.FiscalDoc),
                RTDocMoment = DateTime.Parse(response.AddInfo.DateTimeString),
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data           
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            return receiptResponse;
        }
        catch (Exception e)
        {
            await ResetPrinter();
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    private async Task ResetPrinter()
    {
        _ = await _printerClient.SendCommand<Response<InfoResp>>(new ResetPrinter());
    }

    public static ProcessResponse CreateResponse(ReceiptResponse receiptResponse)
    {
        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse
        };
    }
}
