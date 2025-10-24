using System;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Examples;

/// <summary>
/// Example usage of the ZwarteDoosApiClient
/// </summary>
public class ZwarteDoosApiExample
{
    public static async Task RunExampleAsync()
    {
        // Configure the API client
        var configuration = new ZwarteDoosApiClientConfiguration
        {
            DeviceId = "FDM02030462", // Replace with your actual device ID
            SharedSecret = "6fab7067-bc9e-45fa-bd76-93ed1d1fde3b", // Replace with your actual shared secret
            BaseUrl = "https://sdk.zwartedoos.be",
            TimeoutSeconds = 30
        };

        // Create HTTP client and API client
        using var httpClient = new HttpClient();
        var logger = new ConsoleLogger<ZwarteDoosApiClient>();
        var apiClient = new ZwarteDoosApiClient(configuration, httpClient, logger);

        try
        {
            // Example 1: Get Device Information
            Console.WriteLine("Getting device information...");
            var deviceInfo = await apiClient.GetDeviceIdAsync();
            Console.WriteLine($"Device ID: {deviceInfo.Id}");

            // Example 2: Create and sign an order
            Console.WriteLine("\nCreating and signing an order...");
            
            var orderData = new OrderInputBuilder()
                .WithBasicInfo(
                    vatNo: "BE0000000097",
                    estNo: "2000000042", 
                    posId: "CPOS0031234567",
                    ticketNo: 1003,
                    deviceId: "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                    terminalId: "TER-1-BAR"
                )
                .WithBookingPeriod("dffcd829-a0e5-41ca-a0ae-9eb887f95637")
                .WithEmployee("75061189702")
                .WithCostCenter("T1", "TABLE", "O158")
                .WithPosVersion("1.8.3")
                .AddProduct(
                    productId: "10006",
                    productName: "Dry Martini",
                    departmentId: "10",
                    departmentName: "Aperitifs",
                    quantity: 2,
                    unitPrice: 12,
                    vatLabel: "A",
                    vatPrice: 24
                )
                .AddProduct(
                    productId: "28007",
                    productName: "Tapas variation",
                    departmentId: "28",
                    departmentName: "Tapas",
                    quantity: 4,
                    unitPrice: 3.34m,
                    vatLabel: "B",
                    vatPrice: 13.36m
                )
                .Build();

            var signedOrder = await apiClient.OrderAsync(orderData, isTraining: false);
            
            Console.WriteLine($"Order signed successfully!");
            Console.WriteLine($"POS ID: {signedOrder.PosId}");
            Console.WriteLine($"Fiscal Ticket No: {signedOrder.PosFiscalTicketNo}");
            Console.WriteLine($"Digital Signature: {signedOrder.DigitalSignature}");
            Console.WriteLine($"FDM ID: {signedOrder.FdmRef.FdmId}");
            Console.WriteLine($"Event Counter: {signedOrder.FdmRef.EventCounter}");
            Console.WriteLine($"Total Counter: {signedOrder.FdmRef.TotalCounter}");
            
            if (signedOrder.Warnings.Count > 0)
            {
                Console.WriteLine($"Warnings: {signedOrder.Warnings.Count}");
                foreach (var warning in signedOrder.Warnings)
                {
                    Console.WriteLine($"  - {warning.Message}");
                }
            }

            // Example 3: Generate TurnoverX Report (current turnover)
            Console.WriteLine("\nGenerating TurnoverX report...");
            var turnoverXData = new ReportTurnoverXInput
            {
                VatNo = "BE0000000097",
                EstNo = "2000000042",
                PosId = "CPOS0031234567",
                DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                TerminalId = "TER-1-BAR",
                EmployeeId = "75061189702"
            };

            var turnoverXReport = await apiClient.ReportTurnoverXAsync(turnoverXData, isTraining: false);
            Console.WriteLine($"TurnoverX report generated. FDM ID: {turnoverXReport.FdmRef.FdmId}");

            // Example 4: Generate TurnoverZ Report (finalized turnover)
            Console.WriteLine("\nGenerating TurnoverZ report...");
            var turnoverZData = new ReportTurnoverZInput
            {
                VatNo = "BE0000000097",
                EstNo = "2000000042",
                PosId = "CPOS0031234567",
                DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                TerminalId = "TER-1-BAR",
                EmployeeId = "75061189702"
            };

            var turnoverZReport = await apiClient.ReportTurnoverZAsync(turnoverZData, isTraining: false);
            Console.WriteLine($"TurnoverZ report generated. FDM ID: {turnoverZReport.FdmRef.FdmId}");

            // Example 5: Generate UserX Report (current user statistics)
            Console.WriteLine("\nGenerating UserX report...");
            var userXData = new ReportUserXInput
            {
                VatNo = "BE0000000097",
                EstNo = "2000000042",
                PosId = "CPOS0031234567",
                DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                TerminalId = "TER-1-BAR",
                EmployeeId = "75061189702"
            };

            var userXReport = await apiClient.ReportUserXAsync(userXData, isTraining: false);
            Console.WriteLine($"UserX report generated. FDM ID: {userXReport.FdmRef.FdmId}");

            // Example 6: Generate UserZ Report (finalized user statistics)
            Console.WriteLine("\nGenerating UserZ report...");
            var userZData = new ReportUserZInput
            {
                VatNo = "BE0000000097",
                EstNo = "2000000042",
                PosId = "CPOS0031234567",
                DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                TerminalId = "TER-1-BAR",
                EmployeeId = "75061189702"
            };

            var userZReport = await apiClient.ReportUserZAsync(userZData, isTraining: false);
            Console.WriteLine($"UserZ report generated. FDM ID: {userZReport.FdmRef.FdmId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example of using the API client with dependency injection
    /// </summary>
    public class OrderService
    {
        private readonly ZwarteDoosApiClient _apiClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ZwarteDoosApiClient apiClient, ILogger<OrderService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<string> ProcessOrderAsync(OrderInput orderData, bool isTraining = false)
        {
            try
            {
                _logger.LogInformation("Processing order for POS ID: {PosId}", orderData.PosId);
                
                var result = await _apiClient.OrderAsync(orderData, isTraining);
                
                _logger.LogInformation("Order processed successfully. FDM ID: {FdmId}", result.FdmRef.FdmId);
                
                return result.DigitalSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process order for POS ID: {PosId}", orderData.PosId);
                throw;
            }
        }

        public async Task<string> GenerateTurnoverReportAsync(bool isClosingReport = false, bool isTraining = false)
        {
            try
            {
                var reportData = new ReportTurnoverXInput
                {
                    VatNo = "BE0000000097",
                    EstNo = "2000000042",
                    PosId = "CPOS0031234567",
                    DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                    TerminalId = "TER-1-BAR",
                    EmployeeId = "75061189702"
                };

                SignOrderData result;
                if (isClosingReport)
                {
                    _logger.LogInformation("Generating TurnoverZ (closing) report");
                    var turnoverZData = new ReportTurnoverZInput
                    {
                        VatNo = reportData.VatNo,
                        EstNo = reportData.EstNo,
                        PosId = reportData.PosId,
                        DeviceId = reportData.DeviceId,
                        TerminalId = reportData.TerminalId,
                        EmployeeId = reportData.EmployeeId
                    };
                    result = await _apiClient.ReportTurnoverZAsync(turnoverZData, isTraining);
                }
                else
                {
                    _logger.LogInformation("Generating TurnoverX (current) report");
                    result = await _apiClient.ReportTurnoverXAsync(reportData, isTraining);
                }
                
                _logger.LogInformation("Turnover report generated successfully. FDM ID: {FdmId}", result.FdmRef.FdmId);
                
                return result.DigitalSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate turnover report");
                throw;
            }
        }

        public async Task<string> GenerateUserReportAsync(bool isClosingReport = false, bool isTraining = false)
        {
            try
            {
                var reportData = new ReportUserXInput
                {
                    VatNo = "BE0000000097",
                    EstNo = "2000000042",
                    PosId = "CPOS0031234567",
                    DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
                    TerminalId = "TER-1-BAR",
                    EmployeeId = "75061189702"
                };

                SignOrderData result;
                if (isClosingReport)
                {
                    _logger.LogInformation("Generating UserZ (closing) report");
                    var userZData = new ReportUserZInput
                    {
                        VatNo = reportData.VatNo,
                        EstNo = reportData.EstNo,
                        PosId = reportData.PosId,
                        DeviceId = reportData.DeviceId,
                        TerminalId = reportData.TerminalId,
                        EmployeeId = reportData.EmployeeId
                    };
                    result = await _apiClient.ReportUserZAsync(userZData, isTraining);
                }
                else
                {
                    _logger.LogInformation("Generating UserX (current) report");
                    result = await _apiClient.ReportUserXAsync(reportData, isTraining);
                }
                
                _logger.LogInformation("User report generated successfully. FDM ID: {FdmId}", result.FdmRef.FdmId);
                
                return result.DigitalSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate user report");
                throw;
            }
        }

        public async Task<bool> VerifyDeviceConnectionAsync()
        {
            try
            {
                var deviceInfo = await _apiClient.GetDeviceIdAsync();
                _logger.LogInformation("Device connection verified. Device ID: {DeviceId}", deviceInfo.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify device connection");
                return false;
            }
        }
    }
}

// Simple console logger for example purposes
public class ConsoleLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => new NullScope();
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
    }

    private class NullScope : IDisposable
    {
        public void Dispose() { }
    }
}