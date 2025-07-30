using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using System.Text;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ProtocolCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IProtocolCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        var series = StaticNumeratorStorage.ProFormaSeries;
        series.Numerator++;
        var receiptResponse = request.ReceiptResponse;
        var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
        receiptResponse.ftReceiptIdentification = invoiceNo;
        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = receiptResponse,
        }, invoiceNo, series.LastHash);
        var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
        var qrCode = PortugalReceiptCalculations.CreateProFormaQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
        AddSignatures(series, response, hash, printHash, qrCode);
        series.LastHash = hash;
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddDocumentoNao());
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
    }

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);
}