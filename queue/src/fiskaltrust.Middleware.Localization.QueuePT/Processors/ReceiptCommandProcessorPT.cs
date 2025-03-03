using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Linq.Expressions;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ReceiptRequestValidatorPT
{
    public static void ValidateReceiptOrThrow(ReceiptRequest receiptRequest)
    {
        Console.WriteLine(receiptRequest);
    }
}

public class ReceiptCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, ftSignaturCreationUnitPT signaturCreationUnitPT, IMiddlewareQueueItemRepository readOnlyQueueItemRepository) : IReceiptCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ftSignaturCreationUnitPT _signaturCreationUnitPT = signaturCreationUnitPT;
    private readonly IReadOnlyQueueItemRepository _readOnlyQueueItemRepository = readOnlyQueueItemRepository;
    private const string SIMPLIFIED_INVOICE_SERIES_PREFIX = "ft FS1";
    public static long? SimplifiedInvoiceSeriesNumerator { get; set; }

    private const string CREDIT_NOTE_SERIES_PREFIX = "ft NC1";
    public static long? CreditNoteSeriesNumerator { get; set; }

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase.Case();
        switch (receiptCase)
        {
            case ReceiptCase.UnknownReceipt0x0000:
                return await UnknownReceipt0x0000Async(request);
            case ReceiptCase.PointOfSaleReceipt0x0001:
                return await PointOfSaleReceipt0x0001Async(request);
            case ReceiptCase.PaymentTransfer0x0002:
                return await PaymentTransfer0x0002Async(request);
            case ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003:
                return await PointOfSaleReceiptWithoutObligation0x0003Async(request);
            case ReceiptCase.ECommerce0x0004:
                return await ECommerce0x0004Async(request);
            case ReceiptCase.Protocol0x0005:
                return await Protocol0x0005Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase((long) request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(request.ReceiptRequest);
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            throw new Exception("Void is not supported");
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            if (CreditNoteSeriesNumerator == null)
            {
                var queueItems = (await _readOnlyQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow);
                foreach (var queueItem in queueItems)
                {
                    if (string.IsNullOrEmpty(queueItem.response))
                    {
                        continue;
                    }

                    var lastReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                    if (lastReceiptResponse == null)
                    {
                        continue;
                    }

                    if (!lastReceiptResponse.ftState.HasFlag(State.Success))
                    {
                        continue;
                    }

                    if (lastReceiptResponse.ftReceiptIdentification.StartsWith(CREDIT_NOTE_SERIES_PREFIX))
                    {
                        CreditNoteSeriesNumerator = int.Parse(lastReceiptResponse.ftReceiptIdentification.Split("/")[1]);
                        break;
                    }
                }
            }

            if (CreditNoteSeriesNumerator == null)
            {
                CreditNoteSeriesNumerator = 0;
            }
            CreditNoteSeriesNumerator++;
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, _queuePT.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = CREDIT_NOTE_SERIES_PREFIX + "/" + CreditNoteSeriesNumerator!.ToString()!.PadLeft(4, '0');
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(hash, _queuePT, _signaturCreationUnitPT, request.ReceiptRequest, response.ReceiptResponse);
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "ATCUD",
                Data = _queuePT.ATCUD + "-" + CreditNoteSeriesNumerator,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
            });
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "Hash",
                Data = hash,
                ftSignatureFormat = SignatureFormat.Text.WithPosition(SignatureFormatPosition.AfterHeader),
                ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
            });
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "Hash",
                Data = hash[..4],
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.HashPrint.As<SignatureType>(),
            });
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "Processed by certified program",
                Data = "no.9999/AT",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.CertificationNo.As<SignatureType>(),
            });

            response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
            _queuePT.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
        else
        {
            if (SimplifiedInvoiceSeriesNumerator == null)
            {
                var queueItems = (await _readOnlyQueueItemRepository.GetAsync()).OrderByDescending(x => x.ftQueueRow);
                foreach (var queueItem in queueItems)
                {
                    if (string.IsNullOrEmpty(queueItem.response))
                    {
                        continue;
                    }

                    var lastReceiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                    if (lastReceiptResponse == null)
                    {
                        continue;
                    }

                    if (!lastReceiptResponse.ftState.HasFlag(State.Success))
                    {
                        continue;
                    }


                    if (lastReceiptResponse.ftReceiptIdentification.StartsWith(SIMPLIFIED_INVOICE_SERIES_PREFIX))
                    {
                        SimplifiedInvoiceSeriesNumerator = int.Parse(lastReceiptResponse.ftReceiptIdentification.Split("/")[1]);
                        break;
                    }
                }
            }

            if (SimplifiedInvoiceSeriesNumerator == null)
            {
                SimplifiedInvoiceSeriesNumerator = 0;
            }
            SimplifiedInvoiceSeriesNumerator++;
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, _queuePT.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = SIMPLIFIED_INVOICE_SERIES_PREFIX + "/" + SimplifiedInvoiceSeriesNumerator!.ToString()!.PadLeft(4, '0');
            var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCode(hash, _queuePT, _signaturCreationUnitPT, request.ReceiptRequest, response.ReceiptResponse);
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "ATCUD",
                Data = _queuePT.ATCUD + "-" + SimplifiedInvoiceSeriesNumerator,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
            });
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "Hash",
                Data = hash,
                ftSignatureFormat = SignatureFormat.Text.WithPosition(SignatureFormatPosition.AfterHeader),
                ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
            });
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "Hash",
                Data = hash[..4],
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.HashPrint.As<SignatureType>(),
            });
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "Processed by certified program",
                Data = "no.9999/AT",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.CertificationNo.As<SignatureType>(),
            });

            response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
            _queuePT.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    private async Task LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var queueItems = readOnlyQueueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
        await foreach (var existingQueueItem in queueItems)
        {
            if (string.IsNullOrEmpty(existingQueueItem.response))
            {
                continue;
            }

            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            receiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = referencedResponse
            };
            break;
        }
    }
}
