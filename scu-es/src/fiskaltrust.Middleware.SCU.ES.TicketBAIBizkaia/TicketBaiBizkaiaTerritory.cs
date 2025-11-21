using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using Microsoft.Xades;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAIBizkaia;

public class TicketBaiBizkaiaTerritory : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://www.batuz.eus/fitxategiak/batuz/ticketbai/sinadura_elektronikoaren_zehaztapenak_especificaciones_de_la_firma_electronica_v1_0.pdf";

    public string PolicyDigest => "Quzn98x3PMbSHwbUzaj5f5KOpiH0u8bvmwbbbNkO9Es=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://sarrerak.bizkaia.eus";

    public string SandboxEndpoint => "https://pruesarrerak.bizkaia.eus";

    public string QrCodeValidationEndpoint => "https://batuz.eus/QRTBAI/";

    public string QrCodeSandboxValidationEndpoint => "https://batuz.eus/QRTBAI/";

    public string SubmitInvoices => "/N3B4000M/aurkezpena";

    public string CancelInvoices => "/N3B4000M/aurkezpena";

    public string GetInvoices => "/N3B4001M/kontsulta";

    public string SubmitZuzendu => throw new NotImplementedException();

    public string CancelZuzendu => throw new NotImplementedException();

    public static Bizkaian3Header GenerateHeader(string nif, string nrs, string mode, string ejer)
    {
        var header = new Bizkaian3Header
        {
            inte = new Bizkaian3HeaderInte
            {
                nif = nif,
                nrs = nrs
            },
            drs = new Bizkaian3HeaderDrs
            {
                mode = mode,
                ejer = ejer
            }
        };
        return header;
    }

    public void AddHeaders(TicketBaiRequest request, HttpRequestHeaders headers)
    {
        headers.Add("eus-bizkaia-n3-version", "1.0");
        headers.Add("eus-bizkaia-n3-content-type", "application/xml");
        // TODO which year needs to be transmitted?
        headers.Add("eus-bizkaia-n3-data",
                JsonSerializer.Serialize(GenerateHeader(request.Sujetos.Emisor.NIF, request.Sujetos.Emisor.ApellidosNombreRazonSocial, "240", DateTime.UtcNow.Year.ToString())));
    }


    public string ProcessContent(TicketBaiRequest request, string content)
    {
        var lroeRequest = new LROEPJ240FacturasEmitidasConSGAltaPeticion
        {
            Cabecera = new Cabecera240Type
            {
                Modelo = Modelo240Enum.Item240,
                Capitulo = CapituloModelo240Enum.Item1,
                Subcapitulo = SubcapituloModelo240Enum.Item11,
                SubcapituloSpecified = true,
                Operacion = OperacionEnum.A00,
                Version = IDVersionEnum.Item10,
                Ejercicio = DateTime.UtcNow.Year,
                ObligadoTributario = new NIFPersonaType
                {
                    NIF = request.Sujetos.Emisor.NIF,
                    ApellidosNombreRazonSocial = request.Sujetos.Emisor.ApellidosNombreRazonSocial,
                },
            },
            FacturasEmitidas =
                [
                    new DetalleEmitidaConSGCodificadoType
                {
                    TicketBai = Encoding.UTF8.GetBytes(content)
                }
                ]
        };

        return XmlHelpers.GetXMLIncludingNamespace(lroeRequest);
    }

    public ByteArrayContent GetHttpContent(string content)
    {
        var requestContent = new ByteArrayContent(Compress(content));
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        requestContent.Headers.Add("Content-Encoding", "gzip");

        return requestContent;
    }


    public byte[] Compress(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        using (var compressedStream = new MemoryStream())
        {
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
    }

    public async Task<(bool success, List<(string code, string message)> messages, string response)> GetSuccess(HttpResponseMessage response)
    {
        var lroeContent = await response.Content.ReadAsStringAsync();
        LROEPJ240FacturasEmitidasConSGAltaRespuesta? lroeResponse = null;
        try
        {
            lroeResponse = XmlHelpers.ParseXML<LROEPJ240FacturasEmitidasConSGAltaRespuesta>(lroeContent);
        }
        catch { }
        var type = response.Headers.NonValidated["eus-bizkaia-n3-tipo-respuesta"].ToString();
        if (type == "Incorrecto")
        {
            var messages = new List<(string code, string message)>();

            response.Headers.NonValidated.TryGetValues("eus-bizkaia-n3-codigo-respuesta", out HeaderStringValues code);
            response.Headers.NonValidated.TryGetValues("eus-bizkaia-n3-mensaje-respuesta", out HeaderStringValues message);
            messages.Add((code.ToString(), message.ToString()));

            foreach (var registro in lroeResponse?.Registros ?? [])
            {
                if (registro.SituacionRegistro is not null)
                {
                    messages.Add((registro.SituacionRegistro.CodigoErrorRegistro, $"{registro.SituacionRegistro.DescripcionErrorRegistroES}; {registro.SituacionRegistro.DescripcionErrorRegistroEU}"));
                }
            }
            return (false, messages, lroeContent);
        }

        return (true, [], lroeContent);
    }
}

public class Bizkaian3Header
{
    public string con => "LROE";
    public string apa => "1.1";
    public Bizkaian3HeaderInte? inte { get; set; }
    public Bizkaian3HeaderDrs? drs { get; set; }
}

public class Bizkaian3HeaderInte
{
    public string? nif { get; set; }
    public string? nrs { get; set; }
}

public class Bizkaian3HeaderDrs
{
    public string? mode { get; set; }
    public string? ejer { get; set; }
}