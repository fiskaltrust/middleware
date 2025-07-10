using fiskaltrust.Middleware.Localization.QueuePT.Interface;
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
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ProtocolCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, ftSignaturCreationUnitPT signaturCreationUnitPT, IMiddlewareQueueItemRepository readOnlyQueueItemRepository) : ProcessorPreparation, IProtocolCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
#pragma warning disable
    private readonly ftSignaturCreationUnitPT _signaturCreationUnitPT = signaturCreationUnitPT;
    protected override IMiddlewareQueueItemRepository _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        var series = StaticNumeratorStorage.ProFormaSeries;
        series.Numerator++;
        var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, invoiceNo, series.LastHash);
        response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
        var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
        var qrCode = PortugalReceiptCalculations.CreateProFormaQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
        AddSignatures(series, response, hash, printHash, qrCode);
        series.LastHash = hash;
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    });

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => throw new NotImplementedException();

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "Hash",
            Data = hash,
            ftSignatureFormat = SignatureFormat.Text.WithPosition(SignatureFormatPosition.AfterHeader),
            ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
        });
        response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = $"{printHash} - Processado por programa certificado",
            Data = $"No {CertificationPosSystem.SoftwareCertificateNumber}/AT",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.CertificationNo.As<SignatureType>(),
        });
        response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "",
            Data = "ATCUD: " + series.ATCUD + "-" + series.Numerator,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
        });
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
    }

    public Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
}