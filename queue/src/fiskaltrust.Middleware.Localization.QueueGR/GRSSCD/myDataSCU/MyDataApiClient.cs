using System.Net.Http.Headers;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.v2.Interface;
using Org.BouncyCastle.Asn1.Ocsp;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;

public class MyDataApiClient : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _prodBaseUrl = "https://mydataapi.aade.gr/";
    private readonly string _devBaseUrl = "https://mydataapidev.aade.gr/";

    public MyDataApiClient(string subscriptionKey, string username)
    {
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(_devBaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("aade-user-id", username);
        _httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", subscriptionKey);
    }

    public async Task<GRSSCDInfo> GetInfoAsync() => await Task.FromResult(new GRSSCDInfo());
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var result = await SendInvoicesAsync(request.ReceiptRequest, request.ReceiptResponse);
        request.ReceiptResponse.AddSignatureItem(new SignaturItem
        {
            Caption = "MyDataContent",
            Data = result
        });
        return new ProcessResponse();
    }

    // Generic method to handle XML serialization and API calls
    public async Task<string> SendInvoicesAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var doc = new InvoicesDoc
        {
            invoice =
            [
                new AadeBookInvoiceType
                {
                    uid = receiptResponse.ftQueueItemID, // guid ? maybe queueitemid
                    //mark = (long) Guid.Parse(receiptResponse.ftQueueItemID).ToByteArray(), // receiptiditenfication?
                    //cancelledByMark = null, // only set for cancellation
                    authenticationCode = "TBD", // calculate basedon fields
                    //transmissionFailure = null, // set by backend?
                    issuer = CreateIssuer(), // issuer from masterdataconfig
                    counterpart = null, // recipient.. what to do if anyonymous?
                    paymentMethods = null, // PayItems
                    invoiceHeader = null, // header to be done
                    invoiceDetails = null, // chargeitems
                    taxesTotals = null, // taxes
                    invoiceSummary = null, // summary
                    qrCodeUrl = null, // what to put here
                    otherTransportDetails = null
                }
            ]
        };
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        using var stringWriter = new StringWriter();
        xmlSerializer.Serialize(stringWriter, doc);
        var xmlContent = stringWriter.ToString();
        return xmlContent;
    }

    private string GetUid()
    {
        string vat = "";
        string dateofIssue = "";
        string installatioNumberOfTaxisRegistry = "";
        string typeOfDocument = "";
        string series = "";
        string AA = "";
        string typeOfDeviationDocument = "";

        return $"{vat}-{dateofIssue}-{installatioNumberOfTaxisRegistry}-{typeOfDocument}-{series}-{AA}-{typeOfDeviationDocument}";
    }

    private PartyType CreateIssuer()
    {
        return new PartyType
        {
            vatNumber = "",
            country = CountryType.GR,
            branch = 0,
            name = "fiskaltrust GR",
            address = new AddressType
            {
                street = "fiskaltrust street",
                number = "1",
                city = "Athens",
                postalCode = "12345"
            },
            documentIdNo = "",
            countryDocumentId = CountryType.GR,
            supplyAccountNo = ""
        };
    }
}
