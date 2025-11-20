using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class ftReceiptCaseDataPayload
{
    [JsonPropertyName("GR")]
    public ftReceiptCaseDataGreekPayload? GR { get; set; }
}

public class ftReceiptCaseDataGreekPayload
{
    public string? MerchantVATID { get; set; }
    public string? Series { get; set; }
    public long? AA { get; set; }
    public string? HashAlg { get; set; }
    public string? HashPayload { get; set; }
    
    /// <summary>
    /// MyData override configuration allowing direct control of invoice properties
    /// </summary>
    [JsonPropertyName("mydataoverride")]
    public MyDataOverride? MyDataOverride { get; set; }
}

public class MyDataOverride
{
    /// <summary>
    /// Invoice-level overrides
    /// </summary>
    [JsonPropertyName("invoice")]
    public InvoiceOverride? Invoice { get; set; }
}

public class InvoiceOverride
{
    /// <summary>
    /// Invoice header overrides
    /// </summary>
    [JsonPropertyName("invoiceHeader")]
    public InvoiceHeaderOverride? InvoiceHeader { get; set; }
}

public class InvoiceHeaderOverride
{
    /// <summary>
    /// Invoice type override. Only allowed values: 3.1, 3.2, 6.1, 6.2, 8.1, 8.2, 9.3
    /// Can only be set when the automatically determined invoice type would result in an error.
    /// </summary>
    [JsonPropertyName("invoiceType")]
    public string? InvoiceType { get; set; }

    /// <summary>
    /// VAT payment suspension indicator
    /// </summary>
    [JsonPropertyName("vatPaymentSuspension")]
    public bool? VatPaymentSuspension { get; set; }

    /// <summary>
    /// Self-pricing indicator
    /// </summary>
    [JsonPropertyName("selfPricing")]
    public bool? SelfPricing { get; set; }

    /// <summary>
    /// Dispatch date (format: yyyy-MM-dd)
    /// </summary>
    [JsonPropertyName("dispatchDate")]
    public DateTime? DispatchDate { get; set; }

    /// <summary>
    /// Dispatch time (format: HH:mm:ss)
    /// </summary>
    [JsonPropertyName("dispatchTime")]
    public DateTime? DispatchTime { get; set; }

    /// <summary>
    /// Vehicle number for transport documents
    /// </summary>
    [JsonPropertyName("vehicleNumber")]
    public string? VehicleNumber { get; set; }

    /// <summary>
    /// Move purpose code
    /// </summary>
    [JsonPropertyName("movePurpose")]
    public int? MovePurpose { get; set; }

    /// <summary>
    /// Fuel invoice indicator
    /// </summary>
    [JsonPropertyName("fuelInvoice")]
    public bool? FuelInvoice { get; set; }

    /// <summary>
    /// Special invoice category code
    /// </summary>
    [JsonPropertyName("specialInvoiceCategory")]
    public int? SpecialInvoiceCategory { get; set; }

    /// <summary>
    /// Invoice variation type code
    /// </summary>
    [JsonPropertyName("invoiceVariationType")]
    public int? InvoiceVariationType { get; set; }

    /// <summary>
    /// Other delivery note header information
    /// </summary>
    [JsonPropertyName("otherDeliveryNoteHeader")]
    public OtherDeliveryNoteHeaderOverride? OtherDeliveryNoteHeader { get; set; }

    /// <summary>
    /// Other move purpose title (free text)
    /// </summary>
    [JsonPropertyName("otherMovePurposeTitle")]
    public string? OtherMovePurposeTitle { get; set; }
}

public class OtherDeliveryNoteHeaderOverride
{
    /// <summary>
    /// Loading address
    /// </summary>
    [JsonPropertyName("loadingAddress")]
    public AddressOverride? LoadingAddress { get; set; }

    /// <summary>
    /// Delivery address
    /// </summary>
    [JsonPropertyName("deliveryAddress")]
    public AddressOverride? DeliveryAddress { get; set; }

    /// <summary>
    /// Start shipping branch
    /// </summary>
    [JsonPropertyName("startShippingBranch")]
    public int? StartShippingBranch { get; set; }

    /// <summary>
    /// Complete shipping branch
    /// </summary>
    [JsonPropertyName("completeShippingBranch")]
    public int? CompleteShippingBranch { get; set; }
}

public class AddressOverride
{
    /// <summary>
    /// Street name
    /// </summary>
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    /// <summary>
    /// Street number
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }
}

public class MyDataSCU : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _receiptBaseAddress;
    readonly bool _sandbox;
    private readonly MasterDataConfiguration _masterDataConfiguration;

    public MyDataSCU(string username, string subscriptionKey, string baseAddress, string receiptBaseAddress, bool sandbox, MasterDataConfiguration masterDataConfiguration)
    {
        _receiptBaseAddress = receiptBaseAddress;
        _sandbox = sandbox;
        _masterDataConfiguration = masterDataConfiguration;
        if(sandbox && _masterDataConfiguration?.Account?.VatId == null)
        {
            _masterDataConfiguration = new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                    VatId = "EL123456789"
                }
            };
        }
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(baseAddress)
        };
        _httpClient.DefaultRequestHeaders.Add("aade-user-id", username);
        _httpClient.DefaultRequestHeaders.Add("ocp-apim-subscription-key", subscriptionKey);
    }
    
    public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest)
    {
        return await Task.FromResult(new EchoResponse { Message = echoRequest.Message });
    }
    
    public async Task<GRSSCDInfo> GetInfoAsync() => await Task.FromResult(new GRSSCDInfo());

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        return await ProcessReceiptAsync(request, null);
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null)
    {
        if (string.IsNullOrEmpty(_masterDataConfiguration.Account.VatId))
        {
            request.ReceiptResponse.SetReceiptResponseError("The VATId is not setup correctly for this Queue. Please check the master data configuration in fiskaltrust.Portal.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        var aadFactory = new AADEFactory(_masterDataConfiguration);
        (var doc, var error) = aadFactory.MapToInvoicesDoc(request.ReceiptRequest, request.ReceiptResponse, receiptReferences);
        if (doc == null)
        {
            if (error != null)
            {
                request.ReceiptResponse.SetReceiptResponseError(error.Exception.Message);
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            else
            {
                request.ReceiptResponse.SetReceiptResponseError("Something went wrong while mapping the inbound data. Please check the inbound request.");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning))
        {
            foreach (var item in doc.invoice)
            {
                item.transmissionFailureSpecified = true;
                item.transmissionFailure = 1;
            }
            SignatureItemFactoryGR.AddTransmissionFailure1Signature(request);
        }

        var payload = AADEFactory.GenerateInvoicePayload(doc);
        var response = await _httpClient.PostAsync("/myDataProvider/SendInvoices", new StringContent(payload, Encoding.UTF8, "application/xml"));
        var content = await response.Content.ReadAsStringAsync();

        var governemntApiResponse = new GovernmentApiData
        {
            Protocol = "mydata",
            ProtocolVersion = "1.0",
            Action = response.RequestMessage!.RequestUri!.ToString(),
            ProtocolRequest = payload,
            ProtocolResponse = content
        };

        // We currently only return this in sandbox
        if (request.ReceiptResponse.ftStateData == null && _sandbox)
        {
            request.ReceiptResponse.ftStateData = new MiddlewareSCUGRMyDataState
            {
                GR = new MiddlewareQueueGRState
                {
                    GovernmentApi = governemntApiResponse
                }
            };
        }

        if ((int) response.StatusCode >= 500)
        {
            request.ReceiptResponse.SetReceiptResponseError("Error while sending the request to MyData API. Please check the logs for more details.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        // TODO in case of a payment transfer with a cbpreviousreceipterference we will update the invoice
        if (response.IsSuccessStatusCode)
        {
            var ersult = GetResponse(content);
            if (ersult != null)
            {
                var data = ersult.response[0];
                if (data == null || data.Items == null || data.ItemsElementName == null)
                {
                    request.ReceiptResponse.SetReceiptResponseError("Invalid response from MyData API.");
                    return new ProcessResponse
                    {
                        ReceiptResponse = request.ReceiptResponse
                    };
                }
                else
                {
                    if (data.statusCode.ToLower() == "success")
                    {
                        for (var i = 0; i < data.ItemsElementName.Length; i++)
                        {
                            if (data.ItemsElementName[i] == ItemsChoiceType.qrUrl)
                            {
                                continue;
                                // In the latest API Version mydata returns a QR Code. We don't need it since we are printing our own QR Code. In case
                                // of ERP API based integrations we will still want this to be added.
                                // request.ReceiptResponse.AddSignatureItem(SignatureItemFactoryGR.CreateGRQRCode(data.Items[i].ToString()));
                            }
                            else
                            {
                                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                                {
                                    Data = data.Items[i].ToString() ?? "",
                                    Caption = data.ItemsElementName[i].ToString(),
                                    ftSignatureFormat = SignatureFormat.Text,
                                    ftSignatureType = (SignatureType) ((long) GRConstants.BASE_STATE | (long) SignatureTypesGR.MyDataInfo)
                                });
                            }
                        }

                        request.ReceiptResponse.AddSignatureItem(SignatureItemFactoryGR.CreateGRQRCode($"{_receiptBaseAddress}/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
                        request.ReceiptResponse.ftReceiptIdentification += $"{doc.invoice[0].invoiceHeader.series}-{doc.invoice[0].invoiceHeader.aa}";
                        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && request.ReceiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var receiptCaseDataPayload))
                        {
                            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(receiptCaseDataPayload.GR.HashPayload));
                            SignatureItemFactoryGR.AddHandwrittenReceiptSignature(request, EncodeToUrlSafeBase64(hash), _sandbox);
                        }
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

    public static string EncodeToUrlSafeBase64(byte[] bytes)
    {
        var base64 = Convert.ToBase64String(bytes)
                        .TrimEnd('=')
                        .Replace('+', '-')
                        .Replace('/', '_');
        return base64;
    }

    public class AADEEErrorResponse
    {
        public string? AADEError { get; set; }
        public List<ErrorType> Errors { get; set; } = new List<ErrorType>();
    }

    public ResponseDoc GetResponse(string xmlContent)
    {
        var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
        using var stringReader = new StringReader(xmlContent);
        return (ResponseDoc) xmlSerializer.Deserialize(stringReader)!;
    }

}