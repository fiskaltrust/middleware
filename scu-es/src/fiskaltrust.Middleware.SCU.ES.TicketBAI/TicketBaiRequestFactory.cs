using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using FirmaXadesNetCore;
using FirmaXadesNetCore.Crypto;
using FirmaXadesNetCore.Signature.Parameters;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class TicketBaiRequestFactory
{
    private readonly TicketBaiSCUConfiguration _configuration;
    private readonly ITicketBaiTerritory _ticketBaiTerritory;

    public TicketBaiRequestFactory(TicketBaiSCUConfiguration configuration)
    {
        _configuration = configuration;
        _ticketBaiTerritory = configuration.TicketBaiTerritory switch
        {
            TicketBaiTerritory.Araba => new Araba(),
            TicketBaiTerritory.Bizkaia => new Bizkaia(),
            TicketBaiTerritory.Gipuzkoa => new Gipuzkoa(),
            _ => throw new Exception("Not supported"),
        };
    }

    public string CreateSignedXmlContent<T>(T request)
    {
        var namespaceUri = "urn:ticketbai:emision";
        var prefix = "t";
        var doc = new XmlDocument();
        var nav = doc.CreateNavigator();
        using (var w = nav!.AppendChild())
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(prefix, namespaceUri);
            var ser = new XmlSerializer(typeof(T));
            ser.Serialize(w, request, namespaces);
        }

        var privateKey = _configuration.Certificate.GetRSAPrivateKey();

        var keyInfo = new KeyInfo();
        var keyInfoData = new KeyInfoX509Data();
        keyInfoData.AddCertificate(_configuration.Certificate);
        keyInfo.AddClause(keyInfoData);

        var reference = new Reference("");
        var signedXml = new SignedXml(doc)
        {
            SigningKey = privateKey
        };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        signedXml.KeyInfo = keyInfo;
        signedXml.AddReference(reference);
        signedXml.ComputeSignature();

        var signature = signedXml.GetXml();
        doc.DocumentElement!.AppendChild(signature);

        return doc.OuterXml;
    }

    public string CreateXadesSignedXmlContent<T>(T request)
    {
        var namespaceUri = "urn:ticketbai:emision";
        var prefix = "t";
        var doc = new XmlDocument();
        var nav = doc.CreateNavigator();
        using (var w = nav!.AppendChild())
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(prefix, namespaceUri);
            var ser = new XmlSerializer(typeof(T));
            ser.Serialize(w, request, namespaces);
        }
        return SignXmlContentWithXades(doc.OuterXml);
    }

    public string SignXmlContentWithXades(string xml)
    {
        var xadesService = new XadesService();
        var parameters = new SignatureParameters
        {
            SignaturePolicyInfo = new SignaturePolicyInfo
            {
                PolicyIdentifier = _ticketBaiTerritory.PolicyIdentifier,
                PolicyHash = _ticketBaiTerritory.PolicyDigest,
                PolicyUri = _ticketBaiTerritory.PolicyIdentifier
            },
            SignaturePackaging = SignaturePackaging.ENVELOPED,
            DataFormat = new DataFormat
            {
                MimeType = "text/xml"
            },
            DigestMethod = DigestMethod.SHA256,
            Signer = new Signer(_configuration.Certificate)
        };

        var byteArray = Encoding.ASCII.GetBytes(xml);
        var stream = new MemoryStream(byteArray);
        var signedXmlBytes = xadesService.Sign(stream, parameters).GetDocumentBytes();
        return Encoding.UTF8.GetString(signedXmlBytes, 0, signedXmlBytes.Length);
    }

    public Uri GetQrCodeUri(TicketBaiRequest ticketBaiRequest, TicketBaiResponse ticketBaiResponse)
    {
        var crc8 = new CRC8Calculator();
        var url = $"{_ticketBaiTerritory.QrCodeSandboxValidationEndpoint}?{IdentifierUrl(ticketBaiResponse.Salida.IdentificadorTBAI, ticketBaiRequest)}";
        var cr8 = crc8.ComputeChecksum(url).ToString();
        url += $"&cr={cr8.PadLeft(3, '0')}";
        return new Uri(url);
    }

    private string IdentifierUrl(string ticketBaiIdentifier, TicketBaiRequest ticketBaiRequest)
    {
        return string.Format("id={0}&s={1}&nf={2}&i={3}",
            HttpUtility.UrlEncode(ticketBaiIdentifier),
            HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.SerieFactura),
            HttpUtility.UrlEncode(ticketBaiRequest.Factura.CabeceraFactura.NumFactura),
            HttpUtility.UrlEncode(ticketBaiRequest.Factura.DatosFactura.ImporteTotalFactura)
        );
    }

    public SubmitResponse GetResponseFromContent(string responseContent)
    {
        var ticketBaiResponse = ParseHelpers.ParseXML<TicketBaiResponse>(responseContent) ?? throw new Exception("Something horrible has happened");
        if (ticketBaiResponse.Salida.Estado == "00")
        {
            var identifier = ticketBaiResponse.Salida.IdentificadorTBAI.Split('-');
            var result = new SubmitResponse
            {
                IssuerVatId = identifier[1],
                ExpeditionDate = identifier[2],
                ShortSignatureValue = identifier[3],
                Identifier = ticketBaiResponse.Salida.IdentificadorTBAI,
                Content = responseContent,
                Succeeded = true,
            };
            return result;
        }
        else
        {
            return new SubmitResponse
            {
                Content = responseContent,
                Succeeded = false
            };
        }
    }


}
