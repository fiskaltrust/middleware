using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Xml;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.ES.Helpers;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.Middleware.SCU.ES.Soap;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public class VeriFactuSCUConfiguration
{
    public string BaseUrl { get; set; } = "https://prewww10.aeat.es";
    public string QRCodeBaseUrl { get; set; } = "https://prewww2.aeat.es";

    public X509Certificate2 Certificate { get; set; } = null!;

    public static VeriFactuSCUConfiguration FromConfiguration(PackageConfiguration packageConfiguration)
        => JsonSerializer.Deserialize<VeriFactuSCUConfiguration>(JsonSerializer.Serialize(packageConfiguration.Configuration)) ?? new VeriFactuSCUConfiguration();
}

public class VeriFactuSCU : IESSSCD
{
    private readonly VeriFactuSCUConfiguration _configuration;

    private readonly VeriFactuMapping _veriFactuMapping;

    public VeriFactuSCU(ftSignaturCreationUnitES _, MasterDataConfiguration masterData, VeriFactuSCUConfiguration configuration, IMiddlewareQueueItemRepository queueItemRepository)
    {
        _configuration = configuration;
        _veriFactuMapping = new VeriFactuMapping(masterData, queueItemRepository, configuration.Certificate);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        request.ReceiptResponse.ftReceiptIdentification += $"{request.ReceiptResponse.ftQueueRow}/{request.ReceiptRequest.cbReceiptReference}";

        ReceiptResponse receiptResponse;

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            if (request.PreviousReceiptRequest is null || request.PreviousReceiptResponse is null)
            {
                throw new Exception("There needs to be a previous receipt in the chain to perform a void");
            }

            var journalES = await _veriFactuMapping.CreateRegistroFacturacionAnulacionAsync(request.ReceiptRequest, request.ReceiptResponse, request.PreviousReceiptRequest, request.PreviousReceiptResponse);

            var envelope = new Envelope<RequestBody>
            {
                Body = new RequestBody
                {
                    RegFactuSistemaFacturacion = _veriFactuMapping.CreateRegFactuSistemaFacturacion(journalES)
                }
            };

            receiptResponse = CreateResponse(
                await new Client(new Uri(_configuration.BaseUrl), _configuration.Certificate).SendAsync(envelope),
                request,
                journalES.IDFactura.NumSerieFacturaAnulada,
                journalES.Huella,
                journalES.IDFactura.IDEmisorFacturaAnulada
            );
        }
        else
        {
            var journalES = await _veriFactuMapping.CreateRegistroFacturacionAltaAsync(request.ReceiptRequest, request.ReceiptResponse, request.PreviousReceiptRequest, request.PreviousReceiptResponse);

            var envelope = new Envelope<RequestBody>
            {
                Body = new RequestBody
                {
                    RegFactuSistemaFacturacion = _veriFactuMapping.CreateRegFactuSistemaFacturacion(journalES)
                }
            };

            receiptResponse = CreateResponse(
                await new Client(new Uri(_configuration.BaseUrl), _configuration.Certificate).SendAsync(envelope),
                request,
                journalES.IDFactura.NumSerieFactura,
                journalES.Huella,
                journalES.IDFactura.IDEmisorFactura,
                SignaturItemFactory.CreateVeriFactuQRCode(_configuration.QRCodeBaseUrl + "/wlpl/TIKE-CONT/ValidarQR", journalES)
            );
        }

        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse,
        };
    }

    private ReceiptResponse CreateResponse(
        Result<RespuestaRegFactuSistemaFacturacion, Error> veriFactuResponse,
        ProcessRequest request,
        string numSerieFactura,
        string huella,
        string idEmisorFactura,
        SignatureItem[]? signatureItems = null
        )
    {
        if (veriFactuResponse.IsErr)
        {
            throw new Exception(veriFactuResponse.ErrValue!.ToString());
        }
        var respuesta = veriFactuResponse.OkValue!;
        if (respuesta.EstadoEnvio != EstadoEnvio.Correcto)
        {
            var line = respuesta.RespuestaLinea!.Where(x => x.IDFactura.NumSerieFactura == numSerieFactura).Single();
            throw new Exception($"{respuesta.EstadoEnvio}({line.CodigoErrorRegistro}): {line.DescripcionErrorRegistro}");
        }

        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "Huella",
            Data = huella,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeES.Huella.As<SignatureType>()
        });
        request.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = $"IDEmisorFactura{(request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) ? "Anulada" : null)}",
            Data = idEmisorFactura,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypeES.NIF.As<SignatureType>()
        });


        if (signatureItems is not null)
        {
            foreach (var signatureItem in signatureItems)
            {
                request.ReceiptResponse.AddSignatureItem(signatureItem);
            }
        }

        return request.ReceiptResponse;
    }

    public Task<ESSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
