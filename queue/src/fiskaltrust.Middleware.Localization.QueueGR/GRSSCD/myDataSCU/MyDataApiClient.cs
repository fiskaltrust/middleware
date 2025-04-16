using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;
using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0.MasterData;
using Org.BouncyCastle.Asn1.Ocsp;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;

public class MyDataApiClient : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _prodBaseUrl = "https://mydataapi.aade.gr/";
    private readonly string _devBaseUrl = "https://mydataapidev.aade.gr/";

    private readonly bool _iseinvoiceProvider;
    private readonly bool _isproduction;
    private readonly MasterDataConfiguration _masterDataConfiguration;
    private readonly bool _aadeoffline;

    public static MyDataApiClient CreateClient(Dictionary<string, object> configuration, MasterDataConfiguration masterDataConfiguration)
    {
        var iseinvoiceProvider = false;
        if (configuration.TryGetValue("iseinvoiceProvider", out var data) && bool.TryParse(data?.ToString(), out iseinvoiceProvider))
        { }

        var isproduction = false;
        if (configuration.TryGetValue("production", out var production) && bool.TryParse(production?.ToString(), out isproduction))
        { }

        var aadeoffline = false;
        if (configuration.TryGetValue("aadeoffline", out var aadeofflinedata) && bool.TryParse(aadeofflinedata?.ToString(), out aadeoffline))
        { }
        return new MyDataApiClient(configuration["aade-user-id"].ToString(), configuration["ocp-apim-subscription-key"].ToString(), iseinvoiceProvider, masterDataConfiguration, isproduction, aadeoffline);
    }

    public MyDataApiClient(string username, string subscriptionKey, bool iseinvoiceProvider, MasterDataConfiguration masterDataConfiguration, bool isproduction,  bool aadeoffline = false)
    {
        _iseinvoiceProvider = iseinvoiceProvider;
        _masterDataConfiguration = masterDataConfiguration;
        _aadeoffline = aadeoffline;
        _isproduction = isproduction;
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
        var aadFactory = new AADEFactory(_masterDataConfiguration);
        var doc = aadFactory.MapToInvoicesDoc(request.ReceiptRequest, request.ReceiptResponse);
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning))
        {
            foreach (var item in doc.invoice)
            {
                item.transmissionFailureSpecified = true;
                item.transmissionFailure = 1;
            }

            request.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Data = $"Απώλεια Διασύνδεσης Οντότητας - Παρόχου",
                Caption = "Transmission Failure_1",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
            });
        }

        var payload = aadFactory.GenerateInvoicePayload(doc);
        var path = _iseinvoiceProvider ? "/myDataProvider/SendInvoices" : "/SendReceipts";
        if(_aadeoffline)
        {
            request.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Data = $"Απώλεια Διασύνδεσης Παρόχου – ΑΑΔΕ",
                Caption = "Transmission Failure_2",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
            });
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        var response = await _httpClient.PostAsync(path, new StringContent(payload, Encoding.UTF8, "application/xml"));
        var content = await response.Content.ReadAsStringAsync();
        if ((int) response.StatusCode >= 500)
        {
            // todo should we relaly return this?
            request.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Data = $"Απώλεια Διασύνδεσης Παρόχου – ΑΑΔΕ",
                Caption = "Transmission Failure_2",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
            });
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
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
                            request.ReceiptResponse.AddSignatureItem(CreateGRQRCode(data.Items[i].ToString()));
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
                    if (_iseinvoiceProvider)
                    {
                        if (_isproduction)
                        {
                            request.ReceiptResponse.AddSignatureItem(CreateGRQRCode($"https://viva.receipts.com/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
                        }
                        else
                        {
                            request.ReceiptResponse.AddSignatureItem(CreateGRQRCode($"https://receipts-sandbox.fiskaltrust.eu/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
                        }
                    }

                    request.ReceiptResponse.ftReceiptIdentification += $"{doc.invoice[0].invoiceHeader.series}-{doc.invoice[0].invoiceHeader.aa}";

                    request.ReceiptResponse.AddSignatureItem(new SignatureItem
                    {
                        Data = $"{doc.invoice[0].issuer.vatNumber}|{doc.invoice[0].invoiceHeader.issueDate.ToString("dd/MM/yyyy")}|{doc.invoice[0].issuer.branch}|{doc.invoice[0].invoiceHeader.invoiceType}|{doc.invoice[0].invoiceHeader.series}|{doc.invoice[0].invoiceHeader.aa}",
                        Caption = "Μοναδικός αριιθμός παραστατικού",
                        ftSignatureFormat = SignatureFormat.Text,
                        ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
                    });
                }
                else
                {
                    var errors = data.Items.Cast<ResponseTypeErrors>().SelectMany(x => x.error);
                    request.ReceiptResponse.SetReceiptResponseError(JsonSerializer.Serialize(new AADEEErrorResponse
                    {
                        AADEError = data.statusCode,
                        Errors = errors.ToList()
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

    public static SignatureItem CreateGRQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.gr]",
            Data = qrCode,
            ftSignatureFormat = SignatureFormat.QRCode,
            ftSignatureType = SignatureTypeGR.PosReceipt.As<SignatureType>()
        };
    }

    public ResponseDoc GetResponse(string xmlContent)
    {
        var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
        using var stringReader = new StringReader(xmlContent);
        return (ResponseDoc) xmlSerializer.Deserialize(stringReader);
    }
}
