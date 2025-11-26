using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Text;
using System.Linq;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ReceiptCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IReceiptCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
#pragma warning disable
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
    private readonly RefundValidator _refundValidator = new(readOnlyQueueItemRepository);
    private readonly VoidValidator _voidValidator = new(readOnlyQueueItemRepository);

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => WithPreparations(request, () => PointOfSaleReceipt0x0001Async(request));

    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            var receiptReference = request.ReceiptResponse.GetRequiredPreviousReceiptReference().First();
            request.ReceiptResponse.ftReceiptIdentification += $"{receiptReference.Response.ftReceiptIdentification.Split('#').Last()}";
            // TODO we need to add more signatures
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
            List<Receipt> receiptReferences = response.ReceiptResponse.GetPreviousReceiptReference();
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenceSignature(receiptReferences));
        }
        else
        {
            var qrCode = PortugalReceiptCalculations.CreateQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
            {
                List<Receipt> receiptReferences = response.ReceiptResponse.GetPreviousReceiptReference();
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddProformaReference(receiptReferences));
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

    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            var receiptReference = request.ReceiptResponse.GetRequiredPreviousReceiptReference().First();
            request.ReceiptResponse.ftReceiptIdentification += $"{receiptReference.Response.ftReceiptIdentification.Split('#').Last()}";
            // TODO we need to add more signatures
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
        var qrCode = PortugalReceiptCalculations.CreateVatFreeQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
        AddPaymentTransferSignatures(series, response, hash, printHash, qrCode);
        series.LastHash = hash;
        series.LastCbReceiptMoment = request.ReceiptRequest.cbReceiptMoment;
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            var receiptReferences = response.ReceiptResponse.GetRequiredPreviousReceiptReference();
            AddOrigemReferenceSignature(response, receiptReferences);
        }

        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsPT.HasTransportInformation))
        {
            var validationResult = ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_TransportationIsNotSupported,
                   "EEEE_TransportationIsNotSupported",
                   "ftReceiptCaseFlags"
               ));
            request.ReceiptResponse.SetReceiptResponseError($"Validation error [{validationResult.Errors[0].Code}]: {validationResult.Errors[0].Message} (Field: {validationResult.Errors[0].Field}, Index: {validationResult.Errors[0].ItemIndex})");
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }
        return await PTFallBackOperations.NoOp(request);
    });

    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            var receiptReference = request.ReceiptResponse.GetRequiredPreviousReceiptReference().First();
            request.ReceiptResponse.ftReceiptIdentification += $"{receiptReference.Response.ftReceiptIdentification.Split('#').Last()}";
            // TODO we need to add more signatures
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
        var qrCode = PortugalReceiptCalculations.CreateQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
        AddSignatures(series, response, hash, printHash, qrCode);
        series.LastHash = hash;
        series.LastCbReceiptMoment = request.ReceiptRequest.cbReceiptMoment;
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });


    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsPT.HasTransportInformation))
        {
            var validationResult = ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_TransportationIsNotSupported,
                   "EEEE_TransportationIsNotSupported",
                   "ftReceiptCaseFlags"
               ));
            request.ReceiptResponse.SetReceiptResponseError($"Validation error [{validationResult.Errors[0].Code}]: {validationResult.Errors[0].Message} (Field: {validationResult.Errors[0].Field}, Index: {validationResult.Errors[0].ItemIndex})");
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            var receiptReference = request.ReceiptResponse.GetRequiredPreviousReceiptReference().First();
            request.ReceiptResponse.ftReceiptIdentification += $"{receiptReference.Response.ftReceiptIdentification.Split('#').Last()}";
            // TODO we need to add more signatures
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
        var qrCode = PortugalReceiptCalculations.CreateQRCode(printHash, _queuePT.IssuerTIN, series, request.ReceiptRequest, response.ReceiptResponse);
        AddSignatures(series, response, hash, printHash, qrCode);
        series.LastHash = hash;
        series.LastCbReceiptMoment = request.ReceiptRequest.cbReceiptMoment;
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });

    private static void AddOrigemReferenceSignature(ProcessResponse response, List<Receipt> receiptReferences)
    {
        response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "",
            Data = $"Origem: {receiptReferences[0].Response.ftReceiptIdentification.Split("#").Last()}",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
        });
    }

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreatePTQRCode(qrCode));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddIvaIncluido());
    }

    private static void AddPaymentTransferSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreatePTQRCode(qrCode));
    }

}
