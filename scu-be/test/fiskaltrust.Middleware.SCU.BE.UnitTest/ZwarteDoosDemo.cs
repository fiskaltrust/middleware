using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.UnitTest;

/// <summary>
/// Demo class to showcase ZwarteDoos SCU functionality
/// This can be used for integration testing or manual testing during development
/// </summary>
public class ZwarteDoosDemo
{
    private readonly ZwarteDoosScuBe _scu;
    private readonly ILogger<ZwarteDoosDemo> _logger;

    public ZwarteDoosDemo(ILogger<ZwarteDoosDemo> logger)
    {
        _logger = logger;
        
        var configuration = new ZwarteDoosScuConfiguration
        {
            ServiceUrl = ZwarteDoosConstants.SandboxServiceUrl,
            ApiKey = "demo-api-key",
            CompanyId = "BE0123456789",
            SandboxMode = true,
            TimeoutSeconds = 30,
            EnableLogging = true
        };

        var scuLogger = logger.CreateLogger<ZwarteDoosScuBe>();
        _scu = new ZwarteDoosScuBe(scuLogger, configuration);
    }

    public async Task<SubmitResponse> DemoInvoiceSubmission()
    {
        _logger.LogInformation("Starting demo invoice submission");

        var request = new SubmitInvoiceRequest
        {
            ftCashBoxIdentification = "DEMO-CASHBOX-BE-001",
            InvoiceMoment = DateTime.UtcNow,
            Series = "DEMO",
            InvoiceNumber = $"DEMO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            InvoiceLine = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Description = "Demo Product - Belgian Waffle",
                    Quantity = 2,
                    Amount = 10.00m,
                    VATAmount = 2.10m,
                    VATRate = 21.00m
                },
                new InvoiceLine
                {
                    Description = "Demo Service - Belgian Chocolate",
                    Quantity = 1,
                    Amount = 15.00m,
                    VATAmount = 3.15m,
                    VATRate = 21.00m
                }
            }
        };

        try
        {
            var response = await _scu.SubmitInvoiceAsync(request);
            
            _logger.LogInformation("Demo invoice submission completed. Success: {Success}", response.Succeeded);
            
            if (response.Succeeded)
            {
                _logger.LogInformation("Invoice processed successfully:");
                _logger.LogInformation("- Signature: {Signature}", response.ShortSignatureValue);
                _logger.LogInformation("- Transaction ID: {TransactionId}", response.Identifier);
                _logger.LogInformation("- QR Code: {QrCode}", response.QrCode);
            }
            else
            {
                _logger.LogWarning("Invoice processing failed:");
                foreach (var (code, message) in response.ResultMessages)
                {
                    _logger.LogWarning("- {Code}: {Message}", code, message);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo invoice submission failed with exception");
            throw;
        }
    }

    public async Task DemoStatusCheck()
    {
        _logger.LogInformation("Checking ZwarteDoos service status");

        try
        {
            // Get SCU info using IBESSCD interface
            var info = await _scu.GetInfoAsync();
            _logger.LogInformation("ZwarteDoos SCU info retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service status check failed");
            throw;
        }
    }

    public async Task DemoReceiptProcessing()
    {
        _logger.LogInformation("Starting demo receipt processing through IBESSCD interface");

        try
        {
            // Create a demo receipt request (simplified for demo purposes)
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = "DEMO-CASHBOX-BE-001",
                cbReceiptReference = $"DEMO-RECEIPT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                cbChargeItems = new List<ChargeItem>
                {
                    new ChargeItem
                    {
                        Description = "Belgian Waffle",
                        Quantity = 2.0,
                        Amount = 10.0,
                        VATRate = 21.0
                    }
                }
            };

            var receiptResponse = new ReceiptResponse(); // Simplified demo response

            var processRequest = new ProcessRequest
            {
                ReceiptRequest = receiptRequest,
                ReceiptResponse = receiptResponse
            };

            // Process through IBESSCD interface
            var result = await _scu.ProcessReceiptAsync(processRequest, new List<(ReceiptRequest, ReceiptResponse)>());

            _logger.LogInformation("Demo receipt processing completed successfully");
            _logger.LogInformation("Receipt response received with ID: {ReceiptId}", result.ReceiptResponse.ftReceiptIdentification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo receipt processing failed");
            throw;
        }
    }
}