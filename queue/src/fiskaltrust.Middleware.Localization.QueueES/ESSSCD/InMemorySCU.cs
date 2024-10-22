using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public class ESSSCDInfo
{
}

public class InMemorySCUConfiguration
{

}

public class InMemorySCU : IESSSCD
{
    private readonly ftSignaturCreationUnitES _signaturCreationUnitES;

    public InMemorySCU(ftSignaturCreationUnitES signaturCreationUnitES)
    {
        _signaturCreationUnitES = signaturCreationUnitES;
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(CreateESQRCode(""));

        return await Task.FromResult(new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse,
        });
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

    public static SignatureItem CreateESQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.es]",
            Data = qrCode,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesES.PosReceipt
        };
    }
}
