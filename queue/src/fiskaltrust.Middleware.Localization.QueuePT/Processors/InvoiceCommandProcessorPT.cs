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
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Contracts.Factories;
using Azure;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class InvoiceCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IInvoiceCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        var receiptResponse = request.ReceiptResponse;
        List<(ReceiptRequest, ReceiptResponse)> receiptReferences = [];
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException("The given cbPreviousReceiptReference didn't match with any of the items in the Queue.");
            }
            if (receiptReferences.Count > 1)
            {
                throw new NotSupportedException("Multiple receipt references are currently not supported.");
            }
            receiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = receiptReferences[0].Item2,
            };
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var series = StaticNumeratorStorage.CreditNoteSeries;
            series.Numerator++;
            receiptResponse.ftReceiptIdentification = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = receiptResponse,
            }, series.LastHash);
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddReferenceSignature(receiptReferences));
            series.LastHash = hash;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
        else
        {
            var series = StaticNumeratorStorage.InvoiceSeries;
            series.Numerator++;
            receiptResponse.ftReceiptIdentification = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, series.LastHash);
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
            {
                response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddProformaReference(receiptReferences));
            }
            series.LastHash = hash;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
    });

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.AddIvaIncluido());
    }
}