using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;

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
    public ByteArrayContent GetContent(TicketBaiRequest request, string content)
    {
        var loreRequest = new LROEPJ240FacturasEmitidasConSGAltaPeticion
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
        var rawContent = XmlHelpers.GetXMLIncludingNamespace(loreRequest);
        var requestContent = new ByteArrayContent(Compress(rawContent));
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

    public async Task<Result<string, Result<string, Exception>>> GetResponse(HttpResponseMessage response)
    {
        var loreContent = await response.Content.ReadAsStringAsync();
        var loreResponse = XmlHelpers.ParseXML<LROEPJ240FacturasEmitidasConSGAltaRespuesta>(loreContent);
        if (response.Headers.NonValidated.TryGetValues("eus-bizkaia-n3-tipo-respuesta", out HeaderStringValues type) && type.ToString() == "Incorrecto")
        {
            response.Headers.NonValidated.TryGetValues("eus-bizkaia-n3-codigo-respuesta", out HeaderStringValues code);
            response.Headers.NonValidated.TryGetValues("eus-bizkaia-n3-mensaje-respuesta", out HeaderStringValues messages);

            var error = "";
            var situacionRegistro = loreResponse?.Registros.FirstOrDefault()?.SituacionRegistro;
            if (situacionRegistro is not null)
            {
                error = $"{situacionRegistro.CodigoErrorRegistro}: {situacionRegistro.DescripcionErrorRegistroES}; {situacionRegistro.DescripcionErrorRegistroEU}";
            }
            return new Result<string, Result<string, Exception>>.Err(new Exception($"{code}: {messages}\n{error}"));
        }
        return loreContent;
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