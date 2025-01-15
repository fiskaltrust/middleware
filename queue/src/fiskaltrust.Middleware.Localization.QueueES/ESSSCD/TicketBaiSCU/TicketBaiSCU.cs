using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Xml;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.ES.Helpers;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.Middleware.SCU.ES.Soap;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.SCU.ES.TicketBAI;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public class TicketBaiSCU : IESSSCD
{
    private readonly SCU.ES.TicketBAI.TicketBaiSCU _scu;

    public TicketBaiSCU(ILoggerFactory loggerFactory, ftSignaturCreationUnitES _, TicketBaiSCUConfiguration configuration)
    {
        _scu = new SCU.ES.TicketBAI.TicketBaiSCU(loggerFactory.CreateLogger<SCU.ES.TicketBAI.TicketBaiSCU>(), configuration);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var submitInvoiceRequest = new SubmitInvoiceRequest
        {
            ftCashBoxIdentification = request.ReceiptResponse.ftCashBoxIdentification,
            InvoiceMoment = request.ReceiptResponse.ftReceiptMoment,
            InvoiceNumber = request.ReceiptRequest.cbReceiptReference!, // QUESTION
            LastInvoiceMoment = request.PreviousReceiptResponse?.ftReceiptMoment,
            LastInvoiceNumber = request.PreviousReceiptRequest?.cbReceiptReference,
            LastInvoiceSignature = request.PreviousReceiptResponse?.ftSignatures?.First(x => x.ftSignatureType.IsType(SignatureTypeES.Signature)).Data,
            Series = "",
            InvoiceLine = request.ReceiptRequest.cbChargeItems.Select(c => new InvoiceLine
            {
                Amount = c.Amount,
                Description = c.Description,
                Quantity = c.Quantity,
                VATAmount = c.VATAmount ?? (c.Amount * c.VATRate),
                VATRate = c.VATRate
            }).ToList()
        };

        var submitResponse = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
            ? await _scu.CancelInvoiceAsync(submitInvoiceRequest)
            : await _scu.SubmitInvoiceAsync(submitInvoiceRequest);

        if (!submitResponse.Succeeded)
        {
            throw new AggregateException(submitResponse.ResultMessages.Select(r => new Exception($"{r.code}: {r.message}")));
        }

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = submitResponse.QrCode!.ToString(),
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeES.Url.As<SignatureType>()
        });

        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "Signature",
            Data = submitResponse.ShortSignatureValue!,
            ftSignatureFormat = SignatureFormat.Base64,
            ftSignatureType = SignatureTypeES.Signature.As<SignatureType>()
        });

        foreach (var message in submitResponse.ResultMessages)
        {
            request.ReceiptResponse.AddSignatureItem(new SignatureItem()
            {
                Caption = $"Codigo {message.code}",
                Data = message.message,
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureType.Unknown.WithCategory(SignatureTypeCategory.Information)
            });
        }

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
