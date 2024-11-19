using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public class ESSSCDInfo
{
}

public class InMemorySCUConfiguration
{
    public string BaseUrl { get; set; } = "https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR";

    public static InMemorySCUConfiguration FromConfiguration(PackageConfiguration packageConfiguration)
        => JsonSerializer.Deserialize<InMemorySCUConfiguration>(JsonSerializer.Serialize(packageConfiguration.Configuration)) ?? new InMemorySCUConfiguration();
}

public class InMemorySCU : IESSSCD
{
    // private readonly ftSignaturCreationUnitES _signaturCreationUnitES;

    private readonly InMemorySCUConfiguration _configuration;
    private readonly VeriFactuMapping _veriFactuMapping;

    public InMemorySCU(ftSignaturCreationUnitES _, MasterDataConfiguration masterData, InMemorySCUConfiguration configuration)
    {
        //_signaturCreationUnitES = signaturCreationUnitES;
        _configuration = configuration;
        _veriFactuMapping = new VeriFactuMapping(masterData);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        request.ReceiptResponse.ftReceiptIdentification += $"{request.ReceiptResponse.ftQueueRow}/{request.ReceiptRequest.cbReceiptReference}";
        if (request.ReceiptRequest.IsVoid())
        {
            throw new NotImplementedException();
        }
        else
        {
            var journalES = _veriFactuMapping.CreateRegistroFacturacionAlta(request.ReceiptRequest, request.ReceiptResponse, request.PreviousReceiptRequest is null || request.PreviousReceiptResponse is null ? null : (
                new IDFacturaExpedidaType
                {
                    IDEmisorFactura = request.PreviousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.IDEmisorFactura).Data,
                    NumSerieFactura = request.PreviousReceiptResponse.ftReceiptIdentification,
                    FechaExpedicionFactura = request.PreviousReceiptRequest.cbReceiptMoment.ToString("dd-MM-yyy")
                },
                request.PreviousReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (long) SignatureTypesES.PosReceipt).Data
            ));

            request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateESQRCode(_configuration.BaseUrl, journalES));

            request.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "IDEmisorFactura",
                Data = journalES.Encadenamiento.Item is EncadenamientoFacturaAnteriorType encadenamiento ? encadenamiento.IDEmisorFactura : journalES.IDFactura.IDEmisorFactura,
                ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                ftSignatureType = (long) SignatureTypesES.IDEmisorFactura
            });

            return await Task.FromResult(new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse,
            });
        }
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
