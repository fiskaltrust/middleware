using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class InvoiceCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository, bool sandbox) : ProcessorPreparation, IInvoiceCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly bool _sandbox = sandbox;

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => InvoiceB2C0x1001Async(request);

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => InvoiceB2C0x1001Async(request);

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => InvoiceB2C0x1001Async(request);

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            var receiptReference = request.ReceiptResponse.GetRequiredPreviousReceiptReference().First();
            request.ReceiptResponse.ftReceiptIdentification += $"{receiptReference.Response.ftReceiptIdentification.Split('#').Last()}";
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        var series = await StaticNumeratorStorage.GetNumberSeriesAsync(request.ReceiptRequest, queuePT, await _readOnlyQueueItemRepository);
        series.Numerator++;
        ReceiptIdentificationHelper.AppendSeriesIdentification(request.ReceiptResponse, series);

        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, series.LastHash);

        var printHash = PortugalReceiptCalculations.GetPrintHash(hash);
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) || request.ReceiptRequest.IsPartialRefundReceipt())
        {
            var receiptReferences = response.ReceiptResponse.GetRequiredPreviousReceiptReference();
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, _sandbox, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenceSignature(receiptReferences));
        }
        else
        {
            var qrCode = PortugalReceiptCalculations.CreateQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, _sandbox, qrCode);
            if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
            {
                var receiptReferences = response.ReceiptResponse.GetRequiredPreviousReceiptReference();
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenciaSignature(receiptReferences));
            }
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            SignatureItemFactoryPT.AddHandWrittenSignatures(request, response);
        }
        SignatureItemFactoryPT.AddCustomerSignaturesIfNecessary(request, response);

        series.LastHash = hash;
        if (!request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            series.LastCbReceiptMoment = request.ReceiptRequest.cbReceiptMoment;
        }
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });


    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, bool sandbox, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreatePTQRCode(response, sandbox, qrCode));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddIvaIncluido());
    }
}