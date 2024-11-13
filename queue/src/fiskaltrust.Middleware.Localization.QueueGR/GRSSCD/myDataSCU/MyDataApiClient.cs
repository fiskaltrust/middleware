using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;
using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Org.BouncyCastle.Asn1.Ocsp;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;

public class MyDataApiClient : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _prodBaseUrl = "https://mydataapi.aade.gr/";
    private readonly string _devBaseUrl = "https://mydataapidev.aade.gr/";

    private readonly bool _iseinvoiceProvider;

    public static MyDataApiClient CreateClient(Dictionary<string, object> configuration)
    {
        var iseinvoiceProvider = false;
        if (configuration.TryGetValue("iseinvoiceProvider", out var data) && bool.TryParse(data?.ToString(), out iseinvoiceProvider))
        { }
        return new MyDataApiClient(configuration["aade-user-id"].ToString(), configuration["ocp-apim-subscription-key"].ToString(), iseinvoiceProvider);
    }

    public MyDataApiClient(string username, string subscriptionKey, bool iseinvoiceProvider)
    {
        _iseinvoiceProvider = iseinvoiceProvider;
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
        var aadFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "112545020"
            }
        });
        var doc = aadFactory.MapToInvoicesDoc(request.ReceiptRequest, request.ReceiptResponse);
        if (request.ReceiptRequest.IsLateSigning())
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
                ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                ftSignatureType = (long) SignatureTypesGR.MyDataInfo
            });
        }

        var payload = aadFactory.GenerateInvoicePayload(doc);
        var path = _iseinvoiceProvider ? "/myDataProvider/SendInvoices" : "/SendReceipts";
        var response = await _httpClient.PostAsync(path, new StringContent(payload, Encoding.UTF8, "application/xml"));
        var content = await response.Content.ReadAsStringAsync();
        if((int) response.StatusCode >= 500)
        {
            foreach (var item in doc.invoice)
            {
                item.transmissionFailureSpecified = true;
                item.transmissionFailure = 2;
            }
            if (_iseinvoiceProvider)
            {
                request.ReceiptResponse.AddSignatureItem(CreateGRQRCode($"https://receipts-sandbox.fiskaltrust.eu/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
            }
            request.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Data = $"Απώλεια Διασύνδεσης Παρόχου – ΑΑΔΕ",
                Caption = "Transmission Failure_2",
                ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                ftSignatureType = (long) SignatureTypesGR.MyDataInfo
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
                                ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                                ftSignatureType = (long) SignatureTypesGR.MyDataInfo
                            });
                        }
                    }
                    if (_iseinvoiceProvider)
                    {
                        request.ReceiptResponse.AddSignatureItem(CreateGRQRCode($"https://receipts-sandbox.fiskaltrust.eu/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
                    }

                    request.ReceiptResponse.ftReceiptIdentification += $"{doc.invoice[0].invoiceHeader.series}-{doc.invoice[0].invoiceHeader.aa}";

                    request.ReceiptResponse.AddSignatureItem(new SignatureItem
                    {
                        Data = $"{doc.invoice[0].invoiceHeader.series}",
                        Caption = " Σειρά",
                        ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                        ftSignatureType = (long) SignatureTypesGR.MyDataInfo
                    });

                    request.ReceiptResponse.AddSignatureItem(new SignatureItem
                    {
                        Data = $"{doc.invoice[0].issuer.vatNumber}|{doc.invoice[0].invoiceHeader.issueDate.ToString("dd/MM/yyyy")}|{doc.invoice[0].issuer.branch}|{doc.invoice[0].invoiceHeader.invoiceType}|{doc.invoice[0].invoiceHeader.series}|{doc.invoice[0].invoiceHeader.aa}",
                        Caption = "Αριθμός τιμολογίου",
                        ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
                        ftSignatureType = (long) SignatureTypesGR.MyDataInfo
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
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesGR.PosReceipt
        };
    }

    public ResponseDoc GetResponse(string xmlContent)
    {
        var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
        using var stringReader = new StringReader(xmlContent);
        return (ResponseDoc) xmlSerializer.Deserialize(stringReader);
    }
}
