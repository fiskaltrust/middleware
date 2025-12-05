using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Helpers;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Soap;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu;

public class VeriFactuSCU : IESSSCD
{
    private readonly VeriFactuSCUConfiguration _configuration;
    private readonly IClient _client;
    private readonly VeriFactuMapping _veriFactuMapping;

    public VeriFactuSCU(IClient client, VeriFactuSCUConfiguration configuration)
    {
        _configuration = configuration;
        _veriFactuMapping = new VeriFactuMapping(configuration, signXml: false);
        _client = client;
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        ReceiptResponse receiptResponse;

        if (request.ReceiptResponse.ftStateData is null)
        {
            throw new Exception("ftStateData must be present.");
        }
        var middlewareStateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
        if (middlewareStateData is null || middlewareStateData.ES is null)
        {
            throw new Exception("ES state must be present in ftStateData.");
        }
        GovernmentAPI governmentAPI;

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            if (middlewareStateData.ES.LastReceipt is null)
            {
                throw new Exception("There needs to be a last receipt in the chain to perform a void");
            }

            if (middlewareStateData.PreviousReceiptReference is null || middlewareStateData.PreviousReceiptReference.Count != 1)
            {
                throw new Exception("There needs to be exactly one previous receipt to perform a void");
            }

            var journalES = _veriFactuMapping.CreateRegistroFacturacionAnulacion(request.ReceiptRequest, request.ReceiptResponse, middlewareStateData.ES.LastReceipt.Response, middlewareStateData.PreviousReceiptReference[0].Request, middlewareStateData.PreviousReceiptReference[0].Response);

            var envelope = new Envelope<RequestBody>
            {
                Body = new RequestBody
                {
                    RegFactuSistemaFacturacion = _veriFactuMapping.CreateRegFactuSistemaFacturacion(journalES)
                }
            };
            (var response, governmentAPI) = await _client.SendAsync(envelope);
            receiptResponse = CreateResponse(
                response,
                request,
                middlewareStateData.ES,
                journalES.Huella,
                journalES.IDFactura.IDEmisorFacturaAnulada
            );
        }
        else
        {
            var journalES = _veriFactuMapping.CreateRegistroFacturacionAlta(request.ReceiptRequest, request.ReceiptResponse, middlewareStateData.ES.LastReceipt?.Request, middlewareStateData.ES.LastReceipt?.Response);

            var envelope = new Envelope<RequestBody>
            {
                Body = new RequestBody
                {
                    RegFactuSistemaFacturacion = _veriFactuMapping.CreateRegFactuSistemaFacturacion(journalES)
                }
            };

            (var response, governmentAPI) = await _client.SendAsync(envelope);
            receiptResponse = CreateResponse(
                response,
                request,
                middlewareStateData.ES,
                journalES.Huella,
                journalES.IDFactura.IDEmisorFactura,
                SignaturItemHelper.CreateVeriFactuQRCode(_configuration.QRCodeBaseUrl + "/wlpl/TIKE-CONT/ValidarQR", journalES)
            );
        }

        middlewareStateData.ES.GovernmentAPI = governmentAPI;
        receiptResponse.ftStateData = middlewareStateData;

        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse,
        };
    }

    private ReceiptResponse CreateResponse(
        Result<RespuestaRegFactuSistemaFacturacion, Error> veriFactuResponse,
        ProcessRequest request,
        MiddlewareStateDataES middlewareStateDataES,
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
            var line = respuesta.RespuestaLinea!.Where(x => x.IDFactura.NumSerieFactura == middlewareStateDataES.GetNumSerieFactura()).Single();
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
        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "SerieFactura",
            Data = middlewareStateDataES.SerieFactura,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = ((SignatureType) 0x0005).WithCountry("ES").WithVersion(2).WithFlag(SignatureTypeFlags.DontVisualize)
        });
        request.ReceiptResponse.AddSignatureItem(new SignatureItem()
        {
            Caption = "NumFactura",
            Data = middlewareStateDataES.NumFactura.ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = ((SignatureType) 0x0006).WithCountry("ES").WithVersion(2).WithFlag(SignatureTypeFlags.DontVisualize)
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
    public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => await Task.FromResult(new EchoResponse { Message = echoRequest.Message }).ConfigureAwait(false);
}