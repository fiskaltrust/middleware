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
using System.Security.Cryptography;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class MyDataSCU : IGRSSCD
{
    private readonly HttpClient _httpClient;
    private readonly string _receiptBaseAddress;
    readonly bool _sandbox;
    private readonly MasterDataConfiguration _masterDataConfiguration;
    private readonly ILogger<MyDataSCU> _logger;

    public MyDataSCU(string username, string subscriptionKey, string baseAddress, string receiptBaseAddress, bool sandbox, MasterDataConfiguration masterDataConfiguration, ILogger<MyDataSCU>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(receiptBaseAddress))
        {
            throw new ArgumentException("Receipt base address is required for myDATA v1.0.12", nameof(receiptBaseAddress));
        }

        _receiptBaseAddress = receiptBaseAddress;
        _sandbox = sandbox;
        _masterDataConfiguration = masterDataConfiguration;
        if (sandbox && _masterDataConfiguration?.Account?.VatId == null)
        {
            _masterDataConfiguration = new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                    VatId = "EL123456789"
                }
            };
        }
        _logger = logger ?? NullLogger<MyDataSCU>.Instance;
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
            SetErrorAndLog(request,"The VATId is not setup correctly for this Queue. Please check the master data configuration in fiskaltrust.Portal.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) &&
            request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.DeliveryNote0x0005) &&
            request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation) &&
            receiptReferences != null && receiptReferences.Count > 0)
        {
            var previousReceipt = receiptReferences[0];
            var mark = previousReceipt.Item2.ftSignatures?.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;

            if (string.IsNullOrEmpty(mark))
            {
                SetErrorAndLog(request,"Cannot void delivery note: The mark of the delivery note to cancel is missing. Please provide the mark in the cbPreviousReceiptReference.");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            return await CancelDeliveryNoteAsync(request, mark);
        }

        var hasLocalPayItemFlag = request.ReceiptRequest.cbPayItems.Any(p => ((long) p.ftPayItemCase & 0x0000_0001_0000_0000) != 0);
        if (request.ReceiptRequest.ftReceiptCase.IsCase(ReceiptCase.Pay0x3005) &&
            hasLocalPayItemFlag &&
            receiptReferences != null && receiptReferences.Count > 0)
        {
            var previousReceipt = receiptReferences[0];
            var invoiceMarkText = previousReceipt.Item2.ftSignatures?.FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;

            if (string.IsNullOrEmpty(invoiceMarkText) || !long.TryParse(invoiceMarkText, out var invoiceMark))
            {
                SetErrorAndLog(request,"Cannot send payment method: The invoiceMark of the referenced invoice is missing or invalid. Please ensure cbPreviousReceiptReference points to a successfully submitted invoice.");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            var entityVatNumber = new string(_masterDataConfiguration.Account.VatId.Where(char.IsDigit).ToArray());
            return await SendPaymentsMethodAsync(request, invoiceMark, entityVatNumber);
        }

        var aadFactory = new AADEFactory(_masterDataConfiguration, _receiptBaseAddress);
        (var doc, var error) = aadFactory.MapToInvoicesDoc(request.ReceiptRequest, request.ReceiptResponse, receiptReferences);
        if (doc == null)
        {
            if (error != null)
            {
                SetErrorAndLog(request,error.Exception.Message);
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            else
            {
                SetErrorAndLog(request,"Something went wrong while mapping the inbound data. Please check the inbound request.");
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
            SetErrorAndLog(request,"Error while sending the request to MyData API. Please check the logs for more details.");
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
                    SetErrorAndLog(request,"Invalid response from MyData API.");
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

                            if (data.ItemsElementName[i] == ItemsChoiceType.invoiceUid)
                            {
                                doc.invoice[0].uid = data.Items[i].ToString();
                                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                                {
                                    Data = data.Items[i].ToString() ?? "",
                                    Caption = data.ItemsElementName[i].ToString(),
                                    ftSignatureFormat = SignatureFormat.Text,
                                    ftSignatureType = SignatureTypeGR.Uid.As<SignatureType>()
                                });
                            }
                            else if (data.ItemsElementName[i] == ItemsChoiceType.invoiceMark)
                            {
                                doc.invoice[0].mark = long.Parse(data.Items[i].ToString()!);
                                doc.invoice[0].markSpecified = true;
                                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                                {
                                    Data = data.Items[i].ToString() ?? "",
                                    Caption = data.ItemsElementName[i].ToString(),
                                    ftSignatureFormat = SignatureFormat.Text,
                                    ftSignatureType = SignatureTypeGR.Mark.As<SignatureType>()
                                });
                            }
                            else if (data.ItemsElementName[i] == ItemsChoiceType.authenticationCode)
                            {
                                doc.invoice[0].authenticationCode = data.Items[i].ToString();
                                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                                {
                                    Data = data.Items[i].ToString() ?? "",
                                    Caption = data.ItemsElementName[i].ToString(),
                                    ftSignatureFormat = SignatureFormat.Text,
                                    ftSignatureType = SignatureTypeGR.AuthenticatioNCode.As<SignatureType>()
                                });
                            }
                            else if (data.ItemsElementName[i] == ItemsChoiceType.qrUrl)
                            {
                                // Should we set it?
                                // doc.invoice[0].qrCodeUrl = data.Items[i].ToString();
                                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                                {
                                    Data = data.Items[i].ToString() ?? "",
                                    Caption = data.ItemsElementName[i].ToString(),
                                    ftSignatureFormat = SignatureFormat.QRCode,
                                    ftSignatureType = SignatureTypeGR.QRCode.As<SignatureType>().WithFlag(SignatureTypeFlags.DontVisualize)
                                });
                            }
                            else
                            {
                                request.ReceiptResponse.AddSignatureItem(new SignatureItem
                                {
                                    Data = data.Items[i].ToString() ?? "",
                                    Caption = data.ItemsElementName[i].ToString(),
                                    ftSignatureFormat = SignatureFormat.Text,
                                    ftSignatureType = SignatureTypeGR.GenericMyDataInfo.As<SignatureType>().WithFlag(SignatureTypeFlags.DontVisualize)
                                });
                            }
                        }

                        var enrichedPayload = AADEFactory.GenerateInvoicePayload(doc);
                        // Use the downloadingInvoiceUrl from the invoice for the QR code
                        request.ReceiptResponse.AddSignatureItem(SignatureItemFactoryGR.CreateGRQRCode(doc.invoice[0].downloadingInvoiceUrl));
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
                        SignatureItemFactoryGR.AddMyDataXmlSignature(request, enrichedPayload);
                    }
                    else
                    {
                        var errors = data.Items.Cast<ResponseTypeErrors>().SelectMany(x => x.error);
                        SetErrorAndLog(request,JsonSerializer.Serialize(new AADEEErrorResponse
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
                SetErrorAndLog(request,content);
            }
        }
        else
        {
            SetErrorAndLog(request,content);
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

    private void SetErrorAndLog(ProcessRequest request, string errorMessage)
    {
        _logger.LogError("myDATA error for receipt '{ReceiptReference}' (QueueItemId: {QueueItemId}): {Error}", request.ReceiptRequest.cbReceiptReference, request.ReceiptResponse.ftQueueItemID, errorMessage);
        request.ReceiptResponse.SetReceiptResponseError(errorMessage);
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

    public async Task<ProcessResponse> CancelDeliveryNoteAsync(ProcessRequest request, string mark)
    {
        var vatNumber = new string(_masterDataConfiguration.Account.VatId.Where(char.IsDigit).ToArray());
        var url = $"/myDataProvider/CancelDeliveryNote?mark={mark}&entityVatNumber={vatNumber}";

        var response = await _httpClient.PostAsync(url, null);
        var content = await response.Content.ReadAsStringAsync();

        var governmentApiResponse = new GovernmentApiData
        {
            Protocol = "mydata",
            ProtocolVersion = "1.0",
            Action = response.RequestMessage!.RequestUri!.ToString(),
            ProtocolRequest = "",
            ProtocolResponse = content
        };

        if (request.ReceiptResponse.ftStateData == null && _sandbox)
        {
            request.ReceiptResponse.ftStateData = new MiddlewareSCUGRMyDataState
            {
                GR = new MiddlewareQueueGRState
                {
                    GovernmentApi = governmentApiResponse
                }
            };
        }

        if ((int) response.StatusCode >= 500)
        {
            SetErrorAndLog(request,"Error while sending the cancel request to MyData API.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        if (response.IsSuccessStatusCode)
        {
            var result = GetResponse(content);
            if (result != null && result.response?.Length > 0)
            {
                var data = result.response[0];
                if (data == null)
                {
                    SetErrorAndLog(request,"Invalid response from MyData API.");
                    return new ProcessResponse
                    {
                        ReceiptResponse = request.ReceiptResponse
                    };
                }

                if (data.statusCode.ToLower() == "success")
                {
                    if (data.Items != null && data.ItemsElementName != null)
                    {
                        for (var i = 0; i < data.ItemsElementName.Length; i++)
                        {
                            request.ReceiptResponse.AddSignatureItem(new SignatureItem
                            {
                                Data = data.Items[i].ToString() ?? "",
                                Caption = data.ItemsElementName[i].ToString(),
                                ftSignatureFormat = SignatureFormat.Text,
                                ftSignatureType = (SignatureType) ((long) GRConstants.BASE_STATE | (long) SignatureTypeGR.GenericMyDataInfo)
                            });
                        }
                    }

                    request.ReceiptResponse.AddSignatureItem(SignatureItemFactoryGR.CreateGRQRCode($"{_receiptBaseAddress}/{request.ReceiptResponse.ftQueueID}/{request.ReceiptResponse.ftQueueItemID}"));
                }
                else
                {
                    var errors = data.Items?.Cast<ResponseTypeErrors>().SelectMany(x => x.error);
                    SetErrorAndLog(request,JsonSerializer.Serialize(new AADEEErrorResponse
                    {
                        AADEError = data.statusCode,
                        Errors = errors?.ToList() ?? new List<ErrorType>()
                    }, options: new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }));
                }
            }
            else
            {
                SetErrorAndLog(request,content);
            }
        }
        else
        {
            SetErrorAndLog(request,content);
        }

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    public async Task<ProcessResponse> SendPaymentsMethodAsync(ProcessRequest request, long invoiceMark, string? entityVatNumber = null)
    {
        if (string.IsNullOrEmpty(_masterDataConfiguration.Account.VatId))
        {
            SetErrorAndLog(request,"The VATId is not setup correctly for this Queue. Please check the master data configuration in fiskaltrust.Portal.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        var aadFactory = new AADEFactory(_masterDataConfiguration, _receiptBaseAddress);
        (var doc, var error) = aadFactory.MapToPaymentMethodsDoc(request.ReceiptRequest, invoiceMark, entityVatNumber);
        if (doc == null)
        {
            SetErrorAndLog(request,error?.Exception.Message ?? "Something went wrong while mapping the payment method data. Please check the inbound request.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);
        var response = await _httpClient.PostAsync("/myDataProvider/SendPaymentsMethod", new StringContent(payload, Encoding.UTF8, "application/xml"));
        var content = await response.Content.ReadAsStringAsync();

        var governmentApiResponse = new GovernmentApiData
        {
            Protocol = "mydata",
            ProtocolVersion = "1.0",
            Action = response.RequestMessage!.RequestUri!.ToString(),
            ProtocolRequest = payload,
            ProtocolResponse = content
        };

        if (request.ReceiptResponse.ftStateData == null && _sandbox)
        {
            request.ReceiptResponse.ftStateData = new MiddlewareSCUGRMyDataState
            {
                GR = new MiddlewareQueueGRState
                {
                    GovernmentApi = governmentApiResponse
                }
            };
        }

        if ((int) response.StatusCode >= 500)
        {
            SetErrorAndLog(request,"Error while sending the payment method request to MyData API. Please check the logs for more details.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        if (response.IsSuccessStatusCode)
        {
            var result = GetResponse(content);
            if (result != null && result.response?.Length > 0)
            {
                var data = result.response[0];
                if (data == null || data.Items == null || data.ItemsElementName == null)
                {
                    SetErrorAndLog(request,"Invalid response from MyData API.");
                    return new ProcessResponse
                    {
                        ReceiptResponse = request.ReceiptResponse
                    };
                }

                if (data.statusCode.ToLower() == "success")
                {
                    for (var i = 0; i < data.ItemsElementName.Length; i++)
                    {
                        if (data.ItemsElementName[i] == ItemsChoiceType.qrUrl)
                        {
                            continue;
                        }
                        request.ReceiptResponse.AddSignatureItem(new SignatureItem
                        {
                            Data = data.Items[i].ToString() ?? "",
                            Caption = data.ItemsElementName[i].ToString(),
                            ftSignatureFormat = SignatureFormat.Text,
                            ftSignatureType = (SignatureType) ((long) GRConstants.BASE_STATE | (long) SignatureTypeGR.GenericMyDataInfo)
                        });
                    }
                }
                else
                {
                    var errors = data.Items.Cast<ResponseTypeErrors>().SelectMany(x => x.error);
                    SetErrorAndLog(request,JsonSerializer.Serialize(new AADEEErrorResponse
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
                SetErrorAndLog(request,content);
            }
        }
        else
        {
            SetErrorAndLog(request,content);
        }

        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

}