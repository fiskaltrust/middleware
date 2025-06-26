using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0.MasterData;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;

public class MyDataSCU : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _prodBaseUrl = "https://mydatapi.aade.gr/";
    private readonly string _devBaseUrl = "https://mydataapidev.aade.gr/";
    private readonly string _receiptBaseAddress;
    private readonly MasterDataConfiguration _masterDataConfiguration;

    public MyDataSCU(string username, string subscriptionKey, string baseAddress, string receiptBaseAddress, MasterDataConfiguration masterDataConfiguration)
    {
        _receiptBaseAddress = receiptBaseAddress;
        _masterDataConfiguration = masterDataConfiguration;
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(baseAddress)
        };
        _httpClient.DefaultRequestHeaders.Add("aade-user-id", username);
        _httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", subscriptionKey);
    }

    public async Task<GRSSCDInfo> GetInfoAsync() => await Task.FromResult(new GRSSCDInfo());

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null)
    {
        var aadFactory = new AADEFactory(_masterDataConfiguration);
        var doc = aadFactory.MapToInvoicesDoc(request.ReceiptRequest, request.ReceiptResponse, receiptReferences);
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning))
        {
            foreach (var item in doc.invoice)
            {
                item.transmissionFailureSpecified = true;
                item.transmissionFailure = 1;
            }
            SignatureItemFactoryGR.AddTransmissionFailure1Signature(request);
        }

        var payload = aadFactory.GenerateInvoicePayload(doc);
        var response = await _httpClient.PostAsync("/myDataProvider/SendInvoices", new StringContent(payload, Encoding.UTF8, "application/xml"));
        var content = await response.Content.ReadAsStringAsync();
        if ((int) response.StatusCode >= 500)
        {
            // todo should we relaly return this?
            //request.ReceiptResponse.AddSignatureItem(new SignatureItem
            //{
            //    Data = $"Απώλεια Διασύνδεσης Παρόχου – ΑΑΔΕ",
            //    Caption = "Transmission Failure_2",
            //    ftSignatureFormat = SignatureFormat.Text,
            //    ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
            //});
            throw new Exception("Error while sending the request to MyData API. Please check the logs for more details.");
        }

        // TODO in case of a payment transfer with a cbpreviousreceipterference we will update the invoice
        if (response.IsSuccessStatusCode)
        {
            var ersult = GetResponse(content);
            if (ersult != null)
            {
                var data = ersult.response[0];
                if (data.statusCode.ToLower() == "success")
                {
                    for (var i = 0; i < data.ItemsElementName.Length; i++)
                    {
                        if (data.ItemsElementName[i] == ItemsChoiceType.qrUrl)
                        {
                            request.ReceiptResponse.AddSignatureItem(SignatureItemFactoryGR.CreateGRQRCode(data.Items[i].ToString()));
                        }
                        else
                        {
                            request.ReceiptResponse.AddSignatureItem(new SignatureItem
                            {
                                Data = data.Items[i].ToString(),
                                Caption = data.ItemsElementName[i].ToString(),
                                ftSignatureFormat = SignatureFormat.Text,
                                ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
                            });
                        }
                    }

                    request.ReceiptResponse.AddSignatureItem(SignatureItemFactoryGR.CreateGRQRCode($"{_receiptBaseAddress}/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
                    request.ReceiptResponse.ftReceiptIdentification += $"{doc.invoice[0].invoiceHeader.series}-{doc.invoice[0].invoiceHeader.aa}";
                    if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004))
                    {
                        SignatureItemFactoryGR.AddOrderReceiptSignature(request);
                    }

                    if (doc.invoice[0].invoiceHeader.multipleConnectedMarks?.Length > 0)
                    {
                        SignatureItemFactoryGR.AddMarksForConnectedMarks(request, doc);
                    }
                    SignatureItemFactoryGR.AddInvoiceSignature(request, doc);
                    SignatureItemFactoryGR.AddVivaFiscalProviderSignature(request);
                }
                else
                {
                    var errors = data.Items.Cast<ResponseTypeErrors>().SelectMany(x => x.error);
                    request.ReceiptResponse.SetReceiptResponseError(JsonSerializer.Serialize(new AADEEErrorResponse
                    {
                        AADEError = data.statusCode,
                        Errors = errors.ToList()
                    }, options: new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }));
                }
            }
            else
            {
                request.ReceiptResponse.SetReceiptResponseError(content);
            }
        }
        else
        {
            request.ReceiptResponse.SetReceiptResponseError(content);
        }


        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }


    public class AADEEErrorResponse
    {
        public string AADEError { get; set; }
        public List<ErrorType> Errors { get; set; }
    }


    public ResponseDoc GetResponse(string xmlContent)
    {
        var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
        using var stringReader = new StringReader(xmlContent);
        return (ResponseDoc) xmlSerializer.Deserialize(stringReader);
    }
}
