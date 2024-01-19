using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
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

    public CustomRTPrinterSCU(ILogger<CustomRTPrinterSCU> logger, CustomRTPrinterConfiguration configuration)
    {
        _logger = logger;
        _printerClient = new CustomRTPrinterClient(configuration);
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });
    public override async Task<RTInfo> GetRTInfoAsync()
    {
        var info = await _printerClient.PostAsync<GetInfo, InfoResp>();
        // var info = await _printerClient.PostAsync<QueryPrinterStatus, ?>();

        return new RTInfo
        {
            InfoData = JsonConvert.SerializeObject(info), // TODO: for gods sake don't use newtonsoft
                                                          // TODO: Do we need to map some properties or can the InfoData be anything?
            SerialNumber = info.SerialNumber
        };
    }

    public override Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => throw new NotImplementedException();
}
