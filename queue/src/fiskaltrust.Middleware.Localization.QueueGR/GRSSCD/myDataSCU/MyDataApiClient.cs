using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using Org.BouncyCastle.Asn1.Ocsp;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;

public class MyDataApiClient : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _prodBaseUrl = "https://mydataapi.aade.gr/";
    private readonly string _devBaseUrl = "https://mydataapidev.aade.gr/";

    public MyDataApiClient(string username, string subscriptionKey)
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
        var payload = GenerateInvoicePayload(request.ReceiptRequest, request.ReceiptResponse);
        var response = await _httpClient.PostAsync("/SendInvoices", new StringContent(payload, Encoding.UTF8, "application/xml"));
        var content = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var ersult = GetResponse(content);
            if(ersult != null)
            {
                var data = ersult.response[0];
                for(var i = 0; i < data.ItemsElementName.Length; i++)
                {
                    if (data.ItemsElementName[i] == ItemsChoiceType.qrUrl)
                    {
                        request.ReceiptResponse.AddSignatureItem(CreatePTQRCode(data.Items[i].ToString()));
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

    public static SignatureItem CreatePTQRCode(string qrCode)
    {
        return new SignatureItem()
        {
            Caption = "[www.fiskaltrust.gr]",
            Data = qrCode,
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.QR_Code,
            ftSignatureType = (long) SignatureTypesGR.PosReceipt
        };
    }

    public InvoicesDoc MapToInvoicesDoc(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var invoiceDetails = receiptRequest.cbChargeItems.Select(x => new InvoiceRowType
        {
            quantity = x.Quantity,
            lineNumber = (int) x.Position,
            vatAmount = x.VATAmount ?? 0.0m,
            netValue = x.Amount - (x.VATAmount ?? 0.0m),
            vatCategory = GetVATCategory(x),
            incomeClassification = [
                                    new IncomeClassificationType {
                                        amount =x.Amount - (x.VATAmount ?? 0.0m),
                                        classificationCategory = GetIncomeClassificationCategoryType(x),
                                        classificationType = GetIncomeClassificationValueType(x),
                                        classificationTypeSpecified = true
                                    }
                                ]
        }).ToList();

        var incomeClassificationGroups = invoiceDetails.SelectMany(x => x.incomeClassification).GroupBy(x => (x.classificationCategory, x.classificationType)).Select(x => new IncomeClassificationType
        {
            amount = x.Sum(y => y.amount),
            classificationCategory = x.Key.classificationCategory,
            classificationType = x.Key.classificationType,
            classificationTypeSpecified = true
        }).ToList();

        var doc = new InvoicesDoc
        {
            invoice =
              [
                  new AadeBookInvoiceType
                    {
                        issuer = CreateIssuer(), // issuer from masterdataconfig
                        paymentMethods = receiptRequest.cbPayItems.Select(x => new PaymentMethodDetailType
                          {
                              type = GetPaymentType(x),
                              amount = x.Amount,
                              paymentMethodInfo = x.Description
                          }).ToArray(),
                        invoiceHeader = new InvoiceHeaderType
                        {
                            series = receiptResponse.ftCashBoxIdentification,
                            aa = receiptResponse.ftQueueRow.ToString(),
                            issueDate = receiptRequest.cbReceiptMoment,
                            invoiceType = GetInvoiceType(receiptRequest),
                            currency = CurrencyType.EUR,
                            currencySpecified = true
                        },
                        invoiceDetails = invoiceDetails.ToArray(),
                        invoiceSummary = new InvoiceSummaryType {
                            totalNetValue = receiptRequest.cbChargeItems.Sum(x => x.Amount - (x.VATAmount ?? 0.0m)),
                            totalVatAmount = receiptRequest.cbChargeItems.Sum(x => x.VATAmount ?? 0.0m),
                            totalWithheldAmount = 0.0m,
                            totalFeesAmount = 0.0m,
                            totalStampDutyAmount = 0.0m,
                            totalOtherTaxesAmount = 0.0m,
                            totalDeductionsAmount = 0.0m,
                            totalGrossValue = receiptRequest.cbChargeItems.Sum(x => x.Amount),
                            incomeClassification = incomeClassificationGroups.ToArray()
                        }
                    }
              ]
        };
        return doc;
    }

    private IncomeClassificationValueType GetIncomeClassificationValueType(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
    {
        _ => IncomeClassificationValueType.E3_561_007,
    };

    private IncomeClassificationCategoryType GetIncomeClassificationCategoryType(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
    {
        0x00 => IncomeClassificationCategoryType.category1_2,
        0x10 => IncomeClassificationCategoryType.category1_2,
        0x20 => IncomeClassificationCategoryType.category1_3,
        _ => IncomeClassificationCategoryType.category1_2,
    };

    private int GetVATCategory(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF) switch
    {
        0x3 => 1, // Normal 24%
        0x1 => 2, // Discounted-1 13&
        0x2 => 3, // Discounted-2 6%
        0x4 => 4, // Super reduced 1 17%
        0x5 => 5, // Super reduced 2 9%
        0x6 => 6, // Parking VAT 4%
        0x8 => 7, // Not Taxable
        0x7 => 8, // Zero
        0x0 => 0, // Unknown ???
    };

    private int GetPaymentType(PayItem payItem) => (payItem.ftPayItemCase) switch
    {
        _ => 3, // Cash
    };

    private InvoiceType GetInvoiceType(ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase) switch
    {
        _ => InvoiceType.Item111, // Retail - Simplified Invoice
    };

    // Generic method to handle XML serialization and API calls
    public string GenerateInvoicePayload(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var doc = MapToInvoicesDoc(receiptRequest, receiptResponse);
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        using var stringWriter = new StringWriter();
        xmlSerializer.Serialize(stringWriter, doc);
        var xmlContent = stringWriter.ToString();
        return xmlContent;
    }

    public ResponseDoc GetResponse(string xmlContent)
    {
        var xmlSerializer = new XmlSerializer(typeof(ResponseDoc));
        using var stringReader = new StringReader(xmlContent);
        return (ResponseDoc) xmlSerializer.Deserialize(stringReader);
    }

    private PartyType CreateIssuer()
    {
        return new PartyType
        {
            vatNumber = "997671771",
            country = CountryType.GR,
            branch = 1,
        };
    }
}
