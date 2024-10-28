using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
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

    private readonly VeriFactuMapping _veriFactuMapping;

    public InMemorySCU(ftSignaturCreationUnitES signaturCreationUnitES, MasterDataConfiguration masterData)
    {
        _signaturCreationUnitES = signaturCreationUnitES;
        _veriFactuMapping = new VeriFactuMapping(masterData);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(CreateESQRCode(""));

        if (request.ReceiptRequest.IsVoid())
        {
            throw new NotImplementedException();
        }
        else
        {
            var journalES = _veriFactuMapping.CreateRegistroFacturacionAlta(request.ReceiptRequest, request.ReceiptResponse, request.StateData.EncadenamientoAlta is null ? null : (
                new IDFacturaExpedidaType
                {
                    IDEmisorFactura = request.StateData.EncadenamientoAlta.IDEmisorFactura,
                    NumSerieFactura = request.StateData.EncadenamientoAlta.NumSerieFactura,
                    FechaExpedicionFactura = request.StateData.EncadenamientoAlta.FechaExpedicionFactura
                },
                request.StateData.EncadenamientoAlta.Huella
                )
            );
            return await Task.FromResult(new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse,
                JournalType = "VeriFactu",
                Journal = Encoding.UTF8.GetBytes(journalES.Serialize()),
                StateData = new StateData
                {
                    EncadenamientoAnulacion = request.StateData.EncadenamientoAnulacion,
                    EncadenamientoAlta = new Encadenamiento
                    {
                        IDEmisorFactura = journalES.IDFactura.IDEmisorFactura,
                        NumSerieFactura = journalES.IDFactura.NumSerieFactura,
                        FechaExpedicionFactura = journalES.IDFactura.FechaExpedicionFactura,
                        Huella = journalES.Huella
                    }
                }
            });
        }
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
