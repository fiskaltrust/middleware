using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Invoice;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Financial;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Social;

namespace fiskaltrust.Middleware.SCU.BE.IntegrationTest;

public class ZwarteDoosScuBeIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;
    private readonly ZwarteDoosApiClient _sut; // System Under Test

    public ZwarteDoosScuBeIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        var configuration = new ZwarteDoosApiClientConfiguration
        {
            DeviceId = "FDM02030462",
            SharedSecret = "6fab7067-bc9e-45fa-bd76-93ed1d1fde3b",
            BaseUrl = "https://sdk.zwartedoos.be",
            TimeoutSeconds = 8
        };

        var logger = new XunitLogger<ZwarteDoosApiClient>(_output);
        
        _httpClient = new HttpClient();
        _sut = new ZwarteDoosApiClient(configuration, _httpClient, logger);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldGetDeviceInfo()
    {
        // Act
        var deviceInfo = await _sut.GetDeviceIdAsync();

        // Assert
        deviceInfo.Should().NotBeNull();
        deviceInfo.Data!.SignResult!.Device.Id.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully retrieved device info: {deviceInfo.Data.SignResult!.Device.Id}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateReportTurnoverX()
    {
        // Arrange
        var reportData = new ReportTurnoverXInput
        {
            Language = Language.NL,
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            PosFiscalTicketNo = 1001233,
            PosDateTime = DateTime.Now,
            PosSwVersion = "123",
            TerminalId = "TER-1-BAR",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            BookingPeriodId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            TicketMedium = TicketMedium.PAPER,
            EmployeeId = "75061189702",
            FdmDevices = [],
            PosDevices = [],
            Turnover = new TurnoverInput
            {
                Departments = [],
                Invoices = [],
                NegQuantities = [],
                Payments = [],
                PriceChanges = [],
                Transactions = [],
                DrawersOpenCount = 0,
                Vats = []
            }
        };

        // Act
        var reportResult = await _sut.ReportTurnoverXAsync(reportData, isTraining: false);
        reportResult.Errors.Should().BeNullOrEmpty();

        // Assert
        reportResult.Should().NotBeNull();
        reportResult.Data.Should().NotBeNull();
        reportResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.REPORT_TURNOVER_X);
        reportResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        reportResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        reportResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created TurnoverX report: {reportResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {reportResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateReportTurnoverZ()
    {
        // Arrange
        var reportData = new ReportTurnoverZInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            FdmDevices = [],
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosDevices = [],
            PosFiscalTicketNo = 10012,
            PosSwVersion = "1231",
            TicketMedium = TicketMedium.PAPER,
            Turnover = new TurnoverInput
            {
                Departments = [],
                Invoices = [],
                NegQuantities = [],
                Payments = [],
                PriceChanges = [],
                Transactions = [],
                DrawersOpenCount = 0,
                Vats = []
            },
            ReportBookingDate = DateOnly.FromDateTime(DateTime.Today),
            ReportNo = 1
        };

        // Act
        var reportResult = await _sut.ReportTurnoverZAsync(reportData, isTraining: false);
        reportResult.Errors.Should().BeNullOrEmpty();

        // Assert
        reportResult.Should().NotBeNull();
        reportResult.Data.Should().NotBeNull();
        reportResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.REPORT_TURNOVER_Z);
        reportResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        reportResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        reportResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created TurnoverZ report: {reportResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {reportResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateReportUserX()
    {
        // Arrange
        var reportData = new ReportUserXInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            FdmDevices = [],
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosDevices = [],
            PosFiscalTicketNo = 1003,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
            Users = []
        };

        // Act
        var reportResult = await _sut.ReportUserXAsync(reportData, isTraining: false);
        reportResult.Errors.Should().BeNullOrEmpty(because: JsonSerializer.Serialize(reportResult.Errors, new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        // Assert
        reportResult.Should().NotBeNull();
        reportResult.Data.Should().NotBeNull();
        reportResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.REPORT_USER_X);
        reportResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        reportResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        reportResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created UserX report: {reportResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {reportResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateReportUserZ()
    {
        // Arrange
        var reportData = new ReportUserZInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            FdmDevices = [],
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosDevices = [],
            PosFiscalTicketNo = 10034,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
            Users = [],
            ReportBookingDate = DateOnly.FromDateTime(DateTime.Today),
            ReportNo = 1
        };

        // Act
        var reportResult = await _sut.ReportUserZAsync(reportData, isTraining: false);
        reportResult.Errors.Should().BeNullOrEmpty();

        // Assert
        reportResult.Should().NotBeNull();
        reportResult.Data.Should().NotBeNull();
        reportResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.REPORT_USER_Z);
        reportResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        reportResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        reportResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created UserZ report: {reportResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {reportResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateWorkIn()
    {
        // Arrange
        var workData = new WorkInOutInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosFiscalTicketNo = 10033,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
        };

        // Act
        var workResult = await _sut.WorkInAsync(workData, isTraining: false);
        workResult.Errors.Should().BeNullOrEmpty();

        // Assert
        workResult.Should().NotBeNull();
        workResult.Data.Should().NotBeNull();
        workResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.WORK_IN);
        workResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        workResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        workResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created WorkIn: {workResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {workResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateWorkOut()
    {
        // Arrange
        var workData = new WorkInOutInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosFiscalTicketNo = 1003,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
        };

        // Act
        var workResult = await _sut.WorkOutAsync(workData, isTraining: false);

        // Assert
        workResult.Should().NotBeNull();
        workResult.Data.Should().NotBeNull();
        workResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.WORK_OUT);
        workResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        workResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        workResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created WorkOut: {workResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {workResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateInvoice()
    {
        // Arrange
        var invoiceData = new InvoiceInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            PosFiscalTicketNo = 1004,
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            PosDateTime = DateTime.Now,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            InvoiceNo = "INV-1001",
            CustomerVatNo = "BE0123456749",
            FdmRefs =
            [
                new FdmReferenceInput
                {
                    FdmId = "FDM02030462",
                    EventCounter = 1,
                    EventLabel  = EventLabel.N,
                    FdmDateTime = DateTime.Now.AddMinutes(-10),
                    TotalCounter = 100
                }
            ],
            BookingPeriodId = Guid.NewGuid(),
            CostCenter = new CostCenterInput
            {
                Id = "T1",
                Type = CostCenterType.WEBSHOP,
                Reference = "O158"
            },
            Language = Language.NL,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
        };

        // Act
        var invoiceResult = await _sut.InvoiceAsync(invoiceData, isTraining: false);
        invoiceResult.Errors.Should().BeNullOrEmpty();

        // Assert
        invoiceResult.Should().NotBeNull();
        invoiceResult.Data.Should().NotBeNull();
        invoiceResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.INVOICE);
        invoiceResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        invoiceResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        invoiceResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created invoice: {invoiceResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {invoiceResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateCostCenterChange()
    {
        // Arrange
        var changeData = new CostCenterChangeInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            PosDateTime = DateTime.Now,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Language = Language.NL,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
            PosFiscalTicketNo = 1006,
            Transfer = new TransferInput
            {
                From = 
                [
                    new TransferItemInput
                    {
                        CostCenter = new CostCenterInput
                        {
                            Id = "T1",
                            Type = CostCenterType.TABLE,
                            Reference = "O158"
                        },
                        Transaction = new TransactionInput
                        {
                            TransactionLines = new List<TransactionLineInput>
                            {
                                new TransactionLineInput
                                {
                                    LineType = "SINGLE_PRODUCT",
                                    MainProduct = new ProductInput
                                    {
                                        QuantityType = QuantityType.METER,
                                        ProductId = "10006",
                                        ProductName = "Dry Martini",
                                        DepartmentId = "10",
                                        DepartmentName = "Aperitifs",
                                        Quantity = 1,
                                        UnitPrice = 12,
                                        Vats = new List<VatInput> { new VatInput { Label = VatLabel.A, Price = 12, PriceChanges = [] } }
                                    },
                                    LineTotal = 12
                                }
                            },
                            TransactionTotal = 12
                        }
                    }
                ],
                To =
                [
                    new TransferItemInput
                    {
                        CostCenter = new CostCenterInput
                        {
                            Id = "T1",
                            Type = CostCenterType.TABLE,
                            Reference = "O158"
                        },
                        Transaction = new TransactionInput
                        {
                            TransactionLines = new List<TransactionLineInput>
                            {
                                new TransactionLineInput
                                {
                                    LineType = "SINGLE_PRODUCT",
                                    MainProduct = new ProductInput
                                    {
                                        QuantityType = QuantityType.METER,
                                        ProductId = "10006",
                                        ProductName = "Dry Martini",
                                        DepartmentId = "10",
                                        DepartmentName = "Aperitifs",
                                        Quantity = 1,
                                        UnitPrice = 12,
                                        Vats = new List<VatInput> { new VatInput { Label = VatLabel.A, Price = 12, PriceChanges = [] } }
                                    },
                                    LineTotal = 12
                                }
                            },
                            TransactionTotal = 12
                        }
                    }
                ]
            }
        };

        // Act
        var changeResult = await _sut.CostCenterChangeAsync(changeData, isTraining: false);
        changeResult.Errors.Should().BeNullOrEmpty();

        // Assert
        changeResult.Should().NotBeNull();
        changeResult.Data.Should().NotBeNull();
        changeResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.COST_CENTER_CHANGE);
        changeResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        changeResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        changeResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created cost center change: {changeResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {changeResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreatePreBill()
    {
        // Arrange
        var billData = new PreBillInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            PosFiscalTicketNo = 1005,
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
            CostCenter = new CostCenterInput
            {
                Id = "T1",
                Type = CostCenterType.TABLE,
                Reference = "O158"
            },
            Transaction = new TransactionInput
            {
                TransactionLines = new List<TransactionLineInput>
                {
                    new TransactionLineInput
                    {
                        LineType = "SINGLE_PRODUCT",
                        MainProduct = new ProductInput
                        {
                            QuantityType = QuantityType.METER,
                            ProductId = "10006",
                            ProductName = "Dry Martini",
                            DepartmentId = "10",
                            DepartmentName = "Aperitifs",
                            Quantity = 1,
                            UnitPrice = 12,
                            Vats = new List<VatInput> { new VatInput { Label = VatLabel.A, Price = 12, PriceChanges = [] } }
                        },
                        LineTotal = 12
                    }
                },
                TransactionTotal = 12
            }
        };

        // Act
        var billResult = await _sut.PreBillAsync(billData, isTraining: false);
        billResult.Errors.Should().BeNullOrEmpty();

        // Assert
        billResult.Should().NotBeNull();
        billResult.Data.Should().NotBeNull();
        billResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.PRE_BILL);
        billResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        billResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        billResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created preliminary bill: {billResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {billResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateSale()
    {
        // Arrange
        var saleData = new SaleInput
        {
            Language = Language.NL,
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            PosFiscalTicketNo = 1000,
            PosDateTime = DateTime.UtcNow,
            PosSwVersion = "1.8.3",
            DeviceId = Guid.NewGuid().ToString(),
            TerminalId = "TER-1-BAR",
            BookingPeriodId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            TicketMedium = TicketMedium.PAPER,
            EmployeeId = "75061189702",
            Transaction = new TransactionInput
            {
                TransactionLines =
                [
                    new TransactionLineInput
                    {
                        LineType = "SINGLE_PRODUCT",
                        MainProduct = new ProductInput
                        {
                            ProductId = "10006",
                            ProductName = "Dry Martini",
                            DepartmentId = "10",
                            DepartmentName = "Aperitifs",
                            Quantity = 2,
                            QuantityType = QuantityType.METER,
                            UnitPrice = 12m,
                            Vats = [ new VatInput {Label = VatLabel.A, Price = 24m, PriceChanges = [] } ]
                        },
                        LineTotal = 24m
                    },
                    new TransactionLineInput
                    {
                        LineType = "SINGLE_PRODUCT",
                        MainProduct = new ProductInput
                        {
                            ProductId = "22001",
                            ProductName = "Burger of the Chef",
                            DepartmentId = "22",
                            DepartmentName = "Main Dishes",
                            Quantity = 1,
                            QuantityType = QuantityType.METER,
                            UnitPrice = 28m,
                            Vats = [ new VatInput { Label = VatLabel.B, Price = 28m, PriceChanges = [] } ]
                        },
                        LineTotal = 28m
                    }
                ],
                TransactionTotal = 52m
            },
            Financials =
            [
                new PaymentLineInput
                {
                    Id = "1",
                    Name = "CONTANT",
                    Type = PaymentType.CASH,
                    InputMethod = InputMethod.MANUAL,
                    Amount = 52m,
                    AmountType = PaymentLineType.PAYMENT,
                }
            ]
        };

        // Act
        var saleResult = await _sut.SaleAsync(saleData, isTraining: false);

        // Assert
        saleResult.Should().NotBeNull();
        saleResult.Data.Should().NotBeNull();
        saleResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.SALE);
        saleResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        saleResult.Data!.SignResult!.ShortSignature.Should().NotBeNullOrEmpty();
        saleResult.Data!.SignResult!.VerificationUrl.Should().NotBeNullOrEmpty();
        saleResult.Data!.SignResult!.VatCalc.Should().NotBeNull();
        saleResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        saleResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created sale: {saleResult.Data!.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {saleResult.Data!.SignResult!.DigitalSignature}");
        _output.WriteLine($"Short Signature: {saleResult.Data!.SignResult!.ShortSignature}");
        _output.WriteLine($"Verification URL: {saleResult.Data!.SignResult!.VerificationUrl}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreatePaymentCorrection()
    {
        // Arrange
        var correctionData = new PaymentCorrectionInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            PosDateTime = DateTime.Now,
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Language = Language.NL,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
            Financials = [],
            FdmRef = new FdmReferenceInput
            {
                FdmId = "FDM02030462",
                EventCounter = 1,
                EventLabel = EventLabel.N,
                FdmDateTime = DateTime.Now.AddMinutes(-10),
                TotalCounter = 100
            },
            PosFiscalTicketNo = 1007
        };

        // Act
        var correctionResult = await _sut.PaymentCorrectionAsync(correctionData, isTraining: false);
        correctionResult.Errors.Should().BeNullOrEmpty();

        // Assert
        correctionResult.Should().NotBeNull();
        correctionResult.Data.Should().NotBeNull();
        correctionResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.PAYMENT_CORRECTION);
        correctionResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        correctionResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        correctionResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created payment correction: {correctionResult.Data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {correctionResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateMoneyInOut()
    {
        // Arrange
        var moneyData = new MoneyInOutInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosFiscalTicketNo = 1008,
            PosSwVersion = "123",
            TicketMedium = TicketMedium.PAPER,
            Financials = []
        };

        // Act
        var moneyResult = await _sut.MoneyInOutAsync(moneyData, isTraining: false);

        // Assert
        moneyResult.Should().NotBeNull();
        moneyResult.Data.Should().NotBeNull();
        moneyResult.Data!.SignResult!.EventOperation.Should().Be(EventOperation.MONEY_IN_OUT);
        moneyResult.Data!.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        moneyResult.Data!.SignResult!.FdmRef.Should().NotBeNull();
        moneyResult.Data!.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created money in/out: {moneyResult.Data!.SignResult.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {moneyResult.Data.SignResult!.DigitalSignature}");
    }

    [Fact]
    public async Task ZwarteDoosApiClient_ShouldCreateDrawerOpen()
    {
        // Arrange
        var drawerData = new DrawerOpenInput
        {
            VatNo = "BE0000000097",
            EstNo = "2000000042",
            PosId = "CPOS0031234567",
            DeviceId = "b54a614f-39cc-4a7b-bd9f-aa6b693d769c",
            TerminalId = "TER-1-BAR",
            EmployeeId = "75061189702",
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            BookingPeriodId = Guid.NewGuid(),
            Drawer = new DrawerInput
            {
                Id = "DRAWER-1",
                Name = "Main Cash Drawer"
            },
            Language = Language.NL,
            PosDateTime = DateTime.Now,
            PosSwVersion = "123",
            PosFiscalTicketNo = 1006,
            TicketMedium = TicketMedium.PAPER
        };

        // Act
        var drawerResult = await _sut.DrawerOpenAsync(drawerData, isTraining: false);

        // Assert
        drawerResult.Should().NotBeNull();
        var data = drawerResult.Data!;
        data.SignResult!.EventOperation.Should().Be(EventOperation.DRAWER_OPEN);
        data.SignResult!.DigitalSignature.Should().NotBeNullOrEmpty();
        data.SignResult!.FdmRef.Should().NotBeNull();
        data.SignResult!.FdmRef.FdmId.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Successfully created drawer open: {data.SignResult!.FdmRef.FdmId}");
        _output.WriteLine($"Digital Signature: {data.SignResult!.DigitalSignature}");
    }
}
