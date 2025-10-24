using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosScuBe : IBESSCD, IZwarteDoosScuBe
{
    private readonly ZwarteDoosScuConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ZwarteDoosFactory _zwarteDoosFactory;
    private readonly ILogger<ZwarteDoosScuBe> _logger;

    public ZwarteDoosScuBe(ILogger<ZwarteDoosScuBe> logger, ILoggerFactory loggerFactory, ZwarteDoosScuConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler);
        _zwarteDoosFactory = new ZwarteDoosFactory(
            loggerFactory.CreateLogger<ZwarteDoosFactory>(), 
            _httpClient, 
            _configuration);
    }

    public async Task<SubmitResponse> SubmitInvoiceAsync(SubmitInvoiceRequest request)
    {
        try
        {
            _logger.LogInformation("Processing invoice submission for {InvoiceNumber}", request.InvoiceNumber);

            // Validate request
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                return CreateErrorResponse("Invoice number is required");
            }

            if (string.IsNullOrEmpty(request.ftCashBoxIdentification))
            {
                return CreateErrorResponse("Cash box identification is required");
            }

            // Convert to ZwarteDoos format
            var zwarteDoosRequest = new ZwarteDoosInvoiceRequest
            {
                CompanyId = _configuration.CompanyId,
                InvoiceNumber = request.InvoiceNumber,
                InvoiceDate = request.InvoiceMoment,
                TotalAmount = request.InvoiceLine.Sum(l => l.Amount),
                VatAmount = request.InvoiceLine.Sum(l => l.VATAmount),
                Lines = request.InvoiceLine.Select(line => new ZwarteDoosInvoiceLine
                {
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.Quantity != 0 ? line.Amount / line.Quantity : 0,
                    VatRate = line.VATRate,
                    Amount = line.Amount
                }).ToList()
            };

            // Submit to ZwarteDoos
            var zwarteDoosResponse = await _zwarteDoosFactory.SubmitInvoiceAsync(zwarteDoosRequest);

            // Convert response
            var response = new SubmitResponse
            {
                Succeeded = zwarteDoosResponse.Success,
                RequestContent = System.Text.Json.JsonSerializer.Serialize(zwarteDoosRequest),
                ResponseContent = System.Text.Json.JsonSerializer.Serialize(zwarteDoosResponse),
                ShortSignatureValue = zwarteDoosResponse.Signature,
                Identifier = zwarteDoosResponse.TransactionId,
                ExpeditionDate = zwarteDoosResponse.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                IssuerVatId = _configuration.CompanyId
            };

            if (zwarteDoosResponse.Success)
            {
                response.SignatureValue = new SignatureType
                {
                    Value = zwarteDoosResponse.Signature ?? string.Empty,
                    Algorithm = ZwarteDoosConstants.SignatureAlgorithm
                };

                if (!string.IsNullOrEmpty(zwarteDoosResponse.QrCode))
                {
                    response.QrCode = new Uri(zwarteDoosResponse.QrCode);
                }
            }
            else
            {
                response.ResultMessages = zwarteDoosResponse.Errors
                    .Select(error => ("ERROR", error))
                    .ToList();
            }

            _logger.LogInformation("Invoice submission completed for {InvoiceNumber} with success: {Success}", 
                request.InvoiceNumber, response.Succeeded);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit invoice {InvoiceNumber}", request.InvoiceNumber);
            return CreateErrorResponse($"Internal error: {ex.Message}");
        }
    }

    private static SubmitResponse CreateErrorResponse(string errorMessage)
    {
        return new SubmitResponse
        {
            Succeeded = false,
            ResultMessages = { ("ERROR", errorMessage) }
        };
    }

    // IBESSCD interface implementation
    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request, List<(ReceiptRequest, ReceiptResponse)> receiptReferences)
    {
        try
        {
            _logger.LogInformation("Processing receipt through ZwarteDoos SCU");

            // Convert ReceiptRequest to SubmitInvoiceRequest
            var submitRequest = ConvertReceiptToInvoiceRequest(request.ReceiptRequest);
            
            // Process through ZwarteDoos
            var submitResponse = await SubmitInvoiceAsync(submitRequest);
            
            // Update the receipt response with signature data
            var updatedReceiptResponse = UpdateReceiptResponseWithSignature(request.ReceiptResponse, submitResponse);
            
            return new ProcessResponse
            {
                ReceiptResponse = updatedReceiptResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process receipt through ZwarteDoos SCU");
            
            // Return the original receipt response on error
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }

    public Task<BESSCDInfo> GetInfoAsync()
    {
        _logger.LogInformation("Getting ZwarteDoos SCU info");
        
        var info = new BESSCDInfo();
        // TODO: Populate with ZwarteDoos-specific information
        
        return Task.FromResult(info);
    }

    private SubmitInvoiceRequest ConvertReceiptToInvoiceRequest(ReceiptRequest receiptRequest)
    {
        // Convert ReceiptRequest to SubmitInvoiceRequest format
        // This is a simplified conversion - real implementation would need more detailed mapping
        return new SubmitInvoiceRequest
        {
            ftCashBoxIdentification = receiptRequest.ftCashBoxID?.ToString() ?? "",
            InvoiceMoment = DateTime.UtcNow, // TODO: Extract from receipt
            Series = "REC", // TODO: Extract from receipt
            InvoiceNumber = receiptRequest.cbReceiptReference ?? Guid.NewGuid().ToString(),
            InvoiceLine = receiptRequest.cbChargeItems?.Select(item => new InvoiceLine
            {
                Description = item.Description ?? "Item",
                Quantity = (decimal)item.Quantity,
                Amount = (decimal)item.Amount,
                VATAmount = (decimal)(item.Amount * item.VATRate / 100),
                VATRate = (decimal)item.VATRate
            }).ToList() ?? new List<InvoiceLine>()
        };
    }

    private ReceiptResponse UpdateReceiptResponseWithSignature(ReceiptResponse originalResponse, SubmitResponse submitResponse)
    {
        // Create a copy of the original response and update with signature information
        var updatedResponse = originalResponse; // In real implementation, create proper copy
        
        if (submitResponse.Succeeded && !string.IsNullOrEmpty(submitResponse.ShortSignatureValue))
        {
            // TODO: Update ReceiptResponse with signature data
            // This would involve adding signature information to the appropriate fields
            _logger.LogInformation("Receipt processed successfully with signature: {Signature}", 
                submitResponse.ShortSignatureValue);
        }
        
        return updatedResponse;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}