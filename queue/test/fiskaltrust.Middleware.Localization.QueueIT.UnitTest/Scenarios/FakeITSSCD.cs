using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Scenarios;

/// <summary>
/// Behaves like an RT device behind the ifPOS.v1 <see cref="IITSSCD"/> contract: numbers the documents, counts
/// the Z reports and answers with the signatures of SCU.IT.Abstraction's SignatureFactory.
/// </summary>
internal sealed class FakeITSSCD : IITSSCD
{
    public const string SerialNumber = "96SRT001239";
    private const long BaseState = 0x4954_2000_0000_0000;

    private long _zNumber = 1;
    private long _documentNumber;

    public List<ReceiptRequest> Requests { get; } = [];

    public Task<RTInfo> GetRTInfoAsync() => Task.FromResult(new RTInfo { SerialNumber = SerialNumber, InfoData = "{}" });

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        Requests.Add(request.ReceiptRequest);
        var response = request.ReceiptResponse;
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
        switch (receiptCase)
        {
            case 0x0000:
            case 0x0001:
            case 0x0005:
                var documentType = (request.ReceiptRequest.ftReceiptCase & 0x0100_0000) != 0 ? "REFUND"
                    : (request.ReceiptRequest.ftReceiptCase & 0x0004_0000) != 0 ? "VOID"
                    : "POSRECEIPT";
                response.ftSignatures = [.. response.ftSignatures, .. DocumentSignatures(++_documentNumber, documentType)];
                break;
            case 0x2011:
            case 0x2012:
            case 0x2013:
                response.ftSignatures = [.. response.ftSignatures, Signature(0x11, "<rt-z-number>", _zNumber.ToString().PadLeft(4, '0'))];
                _zNumber++;
                _documentNumber = 0;
                break;
            case 0x3010:
                if (!response.ftSignatures.Any(x => (x.ftSignatureType & 0xFFFF) == 0x21))
                {
                    response.ftState |= 0xEEEE_EEEE;
                    response.ftSignatures = [Signature(0x3000, "FAILURE", "Cannot reprint receipt without references.")];
                }
                break;
            case 0x2000:
                response.ftStateData = "{\"CashStatus\":\"ok\"}";
                break;
        }
        return Task.FromResult(new ProcessResponse { ReceiptResponse = response });
    }

    private SignaturItem[] DocumentSignatures(long documentNumber, string documentType) =>
    [
        Signature(0x10, "<rt-serialnumber>", SerialNumber),
        Signature(0x11, "<rt-z-number>", _zNumber.ToString().PadLeft(4, '0')),
        Signature(0x12, "<rt-doc-number>", documentNumber.ToString().PadLeft(4, '0')),
        Signature(0x13, "<rt-doc-moment>", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
        Signature(0x14, "<rt-document-type>", documentType),
    ];

    private static SignaturItem Signature(long type, string caption, string data) => new()
    {
        Caption = caption,
        Data = data,
        ftSignatureFormat = (long) SignaturItem.Formats.Text,
        ftSignatureType = BaseState | type,
    };

    public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotSupportedException();
    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => throw new NotSupportedException();
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotSupportedException();
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotSupportedException();
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => throw new NotSupportedException();
}

internal sealed class FakeITSSCDClientFactory(IITSSCD client) : IClientFactory<IITSSCD>
{
    public List<ClientConfiguration> Configurations { get; } = [];

    public IITSSCD CreateClient(ClientConfiguration configuration)
    {
        Configurations.Add(configuration);
        return client;
    }
}
