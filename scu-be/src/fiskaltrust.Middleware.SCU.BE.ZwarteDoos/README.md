# ZwarteDoos API Client

This project provides a .NET client for interacting with the ZwarteDoos GraphQL API used for Belgian fiscal device management.

## Overview

The `ZwarteDoosApiClient` class wraps the authentication and communication logic for the ZwarteDoos API, providing a simple and type-safe interface for .NET applications.

## Features

- **Automatic FDM Authentication**: Handles the complex SHA1-based authentication protocol internally
- **Type-safe GraphQL Operations**: Strongly-typed models for requests and responses
- **Fluent Order Builder**: Easy-to-use builder pattern for creating order data
- **Report Generation**: Support for turnover and user reports (X and Z reports)
- **Configurable**: Supports different environments and timeout settings
- **Logging Integration**: Built-in support for Microsoft.Extensions.Logging
- **Async/Await Support**: Modern async programming patterns

## Configuration

```csharp
var configuration = new ZwarteDoosApiClientConfiguration
{
    DeviceId = "your-device-id",        // FDM device identifier
    SharedSecret = "your-shared-secret", // FDM shared secret
    BaseUrl = "https://sdk.zwartedoos.be", // API base URL
    TimeoutSeconds = 30                  // Request timeout
};
```

## Usage

### Basic Usage

```csharp
using var httpClient = new HttpClient();
var logger = new ConsoleLogger<ZwarteDoosApiClient>();
var apiClient = new ZwarteDoosApiClient(configuration, httpClient, logger);

// Get device information
var deviceInfo = await apiClient.GetDeviceIdAsync();
Console.WriteLine($"Device ID: {deviceInfo.Id}");
```

### Creating and Signing Orders

```csharp
// Build order data using the fluent builder
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

// Sign the order
var signedOrder = await apiClient.OrderAsync(orderData, isTraining: false);

Console.WriteLine($"Order signed successfully!");
Console.WriteLine($"Digital Signature: {signedOrder.DigitalSignature}");
Console.WriteLine($"FDM ID: {signedOrder.FdmRef.FdmId}");
```

### Generating Reports

The client supports four types of fiscal reports:

#### TurnoverX Report (Current Turnover)
Reports the turnover so far for the current booking date:

```csharp
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
```

#### TurnoverZ Report (Finalized Turnover)
Reports the finalized turnover after closing a booking date:

```csharp
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
```

#### UserX Report (Current User Statistics)
Reports the operator statistics so far for the current booking date:

```csharp
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
```

#### UserZ Report (Finalized User Statistics)
Reports the finalized operator statistics after closing a booking date:

```csharp
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
```

### Dependency Injection

```csharp
// Register services
services.AddZwarteDoosApiClient(config =>
{
    config.DeviceId = "your-device-id";
    config.SharedSecret = "your-shared-secret";
    config.BaseUrl = "https://sdk.zwartedoos.be";
});

// Use in a service
public class OrderService
{
    private readonly ZwarteDoosApiClient _apiClient;
    
    public OrderService(ZwarteDoosApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async Task<string> ProcessOrderAsync(OrderInput orderData)
    {
        var result = await _apiClient.OrderAsync(orderData);
        return result.DigitalSignature;
    }

    public async Task<string> GenerateTurnoverReportAsync(bool isClosingReport = false)
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
            var turnoverZData = new ReportTurnoverZInput
            {
                VatNo = reportData.VatNo,
                EstNo = reportData.EstNo,
                PosId = reportData.PosId,
                DeviceId = reportData.DeviceId,
                TerminalId = reportData.TerminalId,
                EmployeeId = reportData.EmployeeId
            };
            result = await _apiClient.ReportTurnoverZAsync(turnoverZData);
        }
        else
        {
            result = await _apiClient.ReportTurnoverXAsync(reportData);
        }
        
        return result.DigitalSignature;
    }
}
```

## API Methods

### GetDeviceIdAsync()
Retrieves device information from the ZwarteDoos API.

**Returns**: `DeviceInfo` containing the device ID

### OrderAsync(orderData, isTraining)
Signs an order using the ZwarteDoos fiscal device.

**Parameters**:
- `orderData`: Order information (use `OrderInputBuilder` for easy construction)
- `isTraining`: Boolean indicating if this is a training transaction

**Returns**: `SignOrderData` containing the fiscal signature and metadata

### ReportTurnoverXAsync(reportData, isTraining)
Generates a turnover report for the current booking date (X-report).

**Parameters**:
- `reportData`: Report information (`ReportTurnoverXInput`)
- `isTraining`: Boolean indicating if this is a training transaction

**Returns**: `SignOrderData` containing the fiscal signature and metadata

### ReportTurnoverZAsync(reportData, isTraining)
Generates a finalized turnover report after closing a booking date (Z-report).

**Parameters**:
- `reportData`: Report information (`ReportTurnoverZInput`)
- `isTraining`: Boolean indicating if this is a training transaction

**Returns**: `SignOrderData` containing the fiscal signature and metadata

### ReportUserXAsync(reportData, isTraining)
Generates a user statistics report for the current booking date (X-report).

**Parameters**:
- `reportData`: Report information (`ReportUserXInput`)
- `isTraining`: Boolean indicating if this is a training transaction

**Returns**: `SignOrderData` containing the fiscal signature and metadata

### ReportUserZAsync(reportData, isTraining)
Generates finalized user statistics report after closing a booking date (Z-report).

**Parameters**:
- `reportData`: Report information (`ReportUserZInput`)
- `isTraining`: Boolean indicating if this is a training transaction

**Returns**: `SignOrderData` containing the fiscal signature and metadata

## Authentication

The client automatically handles the FDM authentication protocol:

1. Constructs authentication string: `"POST" + GMT_DATE + SHARED_SECRET + REQUEST_BODY`
2. Computes SHA1 hash of the authentication string
3. Base64 encodes the hash
4. Adds appropriate headers to the HTTP request

## Error Handling

The client throws the following exceptions:

- `HttpRequestException`: For HTTP-level errors (network, server errors)
- `InvalidOperationException`: For GraphQL errors or invalid responses
- `ArgumentException`: For invalid configuration parameters

## Models

### OrderInput
The main order data structure containing:
- Basic transaction information (VAT number, establishment number, POS ID, etc.)
- Transaction lines with product details
- VAT information
- Employee and cost center data

### Report Input Models
- `ReportTurnoverXInput`: Input for current turnover reports
- `ReportTurnoverZInput`: Input for finalized turnover reports
- `ReportUserXInput`: Input for current user statistics reports
- `ReportUserZInput`: Input for finalized user statistics reports

All report input models contain:
- VAT number, establishment number, POS ID
- Device ID, terminal ID, employee ID

### SignOrderData
The response from signing an order or generating a report, containing:
- Digital signature
- FDM reference data (ID, counters, timestamps)
- Warnings and informational messages
- Fiscal receipt footer

## Examples

See the `Examples/ZwarteDoosApiExample.cs` file for complete working examples.

## Integration Tests

The project includes integration tests that demonstrate:
- FDM authentication protocol
- Device information retrieval
- Order creation and signing
- Report generation (TurnoverX, TurnoverZ, UserX, UserZ)

Run tests with: `dotnet test`

## Dependencies

- .NET 8.0
- System.Text.Json
- Microsoft.Extensions.Logging
- System.Net.Http

## License

This code is part of the fiskaltrust middleware project and follows the same licensing terms.