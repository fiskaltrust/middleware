using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using System.Text;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Constants;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

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
        var staticNumberStorage = await StaticNumeratorStorage.GetStaticNumeratorStorageAsync(queuePT, await _readOnlyQueueItemRepository);
        var receiptResponse = request.ReceiptResponse;
        List<(ReceiptRequest, ReceiptResponse)> receiptReferences = [];
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException(ErrorMessagesPT.PreviousReceiptReferenceNotFound);
            }
            if (receiptReferences.Count > 1)
            {
                throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
            }
            receiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = receiptReferences[0].Item2,
            };
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var series = staticNumberStorage.CreditNoteSeries;
            series.Numerator++;
            ReceiptIdentificationHelper.AppendSeriesIdentification(receiptResponse, series);
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = receiptResponse,
            }, series.LastHash);
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenceSignature(receiptReferences));

            if (request.ReceiptRequest.cbCustomer is null)
            {
                response.ReceiptResponse.AddSignatureItem(new SignatureItem
                {
                    Caption = "",
                    Data = $"Consumidor final",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
                });
            }
            series.LastHash = hash;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
        else
        {
            var series = staticNumberStorage.InvoiceSeries;
            series.Numerator++;
            ReceiptIdentificationHelper.AppendSeriesIdentification(receiptResponse, series);
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
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddProformaReference(receiptReferences));
            }

            if (request.ReceiptRequest.cbCustomer is null)
            {
                response.ReceiptResponse.AddSignatureItem(new SignatureItem
                {
                    Caption = "",
                    Data = $"Consumidor final",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
                });
            }

            series.LastHash = hash;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
    });

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreatePTQRCode(qrCode));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddIvaIncluido());
    }
}