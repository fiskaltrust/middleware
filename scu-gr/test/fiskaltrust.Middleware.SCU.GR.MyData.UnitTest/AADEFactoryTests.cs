using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class AADEFactoryTests
{
    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_IfHandwritten_AndFieldsAreNotDefined()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {

                }
            }
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error!.Exception.Message.Should().Be("When using Handwritten receipts the Series must be provided in the ftReceiptCaseData payload.");
    }

    [Fact]
    public void MapToInvoicesDoc_Should_Not_ThrowException_IfHandwritten_FieldsAreDefined_HashPayloadShouldMatch()
    {
        var dateTime = new DateTime(2025, 12, 15, 12, 13, 14, DateTimeKind.Utc);
        var receiptReference = Guid.NewGuid().ToString();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = dateTime,
            cbReceiptReference = receiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = "Test",
                    Series = "test", // This should be defined
                    AA = 123456789,
                    HashAlg = "SHA256",
                    HashPayload = $"WRONG HASHPAYLOAD"
                }
            },
            cbReceiptAmount = 100,
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().NotBeNull();
    }
    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_IfHandwritten_FieldsAreDefined_HashPayloadMatches_EverythingGreen()
    {
        var dateTime = new DateTime(2025, 12, 15, 15, 13, 14, DateTimeKind.Utc);
        var merchantId = "Test";
        var series = "test";
        var aa = 1;
        var cbReceiptReference = Guid.NewGuid().ToString();
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = dateTime,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem 
                {
                    Amount = 100
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = merchantId,
                    Series = series, // This should be defined
                    AA = aa,
                    HashAlg = "SHA256",
                    HashPayload = merchantId + "-" + series + "-" + aa + "-" + cbReceiptReference + "-2025-12-15T15:13:14Z-" + chargeItems.Sum(x => x.Amount)
                }
            },
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = merchantId // This should match the MerchantVATID
            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().BeNull();

        action.Should().NotBeNull();
        action!.invoice[0].invoiceHeader.series.Should().Be(series);
        action!.invoice[0].invoiceHeader.aa.Should().Be(aa.ToString());
    }

    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_FieldsAreDefined_ButShouldBeIgnored()
    {
        var merchantId = "Test";
        var series = "test";
        var aa = 1;
        var cbReceiptReference = Guid.NewGuid().ToString();
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = merchantId,
                    Series = series, // This should be defined
                    AA = aa,
                    HashAlg = "SHA256",
                    HashPayload = merchantId + "-" + series + "-" + aa + "-" + cbReceiptReference + "-" + chargeItems.Sum(x => x.Amount)
                }
            },
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().BeNull();

        action.Should().NotBeNull();
        action!.invoice[0].invoiceHeader.series.Should().Be(receiptResponse.ftCashBoxIdentification);
        action!.invoice[0].invoiceHeader.aa.Should().Be(291.ToString());
    }

    [Fact]
    public void MapToInvoicesDoc_Should_Not_ThrowException_IfHandwritten_AndMerchantVATMissmatch()
    {
        var dateTime = new DateTime(2025, 12, 15, 12, 13, 14, DateTimeKind.Utc);
        var receiptReference = Guid.NewGuid().ToString();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = dateTime,
            cbReceiptReference = receiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = "Test",
                    Series = "test", // This should be defined
                    AA = 123456789,
                    HashAlg = "SHA256",
                    HashPayload = $"WRONG HASHPAYLOAD"
                }
            },
            cbReceiptAmount = 100,
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "WRONG_VAT_ID" 
            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error!.Exception.Message.Should().Be("When using Handwritten receipts the MerchantVATID that is provided must match with the one configured in the Account.");
    }

    [Fact]
    public void MapToInvoiceDoc_ForReceiptRequest_ShouldWork()
    {
        var merchantId = "Test";
        var series = "test";
        var aa = 1;
        var cbReceiptReference = Guid.NewGuid().ToString();
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    MerchantVATID = merchantId,
                    Series = series, // This should be defined
                    AA = aa,
                    HashAlg = "SHA256",
                    HashPayload = merchantId + "-" + series + "-" + aa + "-" + cbReceiptReference + "-" + chargeItems.Sum(x => x.Amount)
                }
            },
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {

            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().BeNull();

        action.Should().NotBeNull();
        action!.invoice[0].invoiceHeader.series.Should().Be(receiptResponse.ftCashBoxIdentification);
        action!.invoice[0].invoiceHeader.aa.Should().Be(291.ToString());
    }

    [Fact]
    public void MapToInvoicesDoc_ForVATExemptReceiptWithCashPayment_ShouldWork()
    {
        var cbReceiptReference = "8-1752270167120";
        var receiptMoment = DateTime.Parse("2025-07-25T07:05:21.392Z").ToUniversalTime();
        var ftPosSystemId = Guid.Parse("7b5955e3-4944-4ff3-8df9-46166b70132a");
        var ftCashBoxID = Guid.Parse("31f3defc-275d-4b6e-9f3f-fa09d64c1bb4");

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = ftCashBoxID,
            cbTerminalID = "",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = ftPosSystemId,
            cbArea = "0",
            cbUser = "Chef",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Πελάτης A.E.",
                CustomerId = null,
                CustomerType = null,
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerZip = null,
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
                CustomerVATId = "026883248"
            },
            cbPreviousReceiptReference = "7-1752270167120",
            ftReceiptCase = (ReceiptCase)0x4752200000000002,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 8,
                    Description = "Grilled Chicken Sandwich",
                    ProductNumber = "004",
                    Quantity = 1,
                    VATRate = 0,
                    ftChargeItemCase = (ChargeItemCase)0x4752200000000098,
                    Moment = DateTime.Parse("2025-07-25T07:05:21.392Z").ToUniversalTime(),
                    Position = 1,
                    VATAmount = 0,
                    Unit = "kg"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Moment = DateTime.Parse("2025-07-25T07:05:29.600Z").ToUniversalTime(),
                    Description = "Cash",
                    Amount = 8,
                    ftPayItemCase = (PayItemCase)0x4752200000000001
                }
            },
            cbReceiptAmount = 8
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "112545020"
            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        
        // Verify no error occurred
        error.Should().BeNull();

        // Verify invoice was created
        action.Should().NotBeNull();
        action!.invoice.Should().HaveCount(1);
        
        var invoice = action.invoice[0];
        
        // Verify basic invoice header
        invoice.invoiceHeader.series.Should().Be(receiptResponse.ftCashBoxIdentification);
        invoice.invoiceHeader.aa.Should().Be("291"); // This is based on the hexadecimal conversion
        invoice.invoiceHeader.issueDate.Should().Be(receiptMoment);
        invoice.invoiceHeader.currency.Should().Be(CurrencyType.EUR);
        
        // Verify customer information
        invoice.counterpart.Should().NotBeNull();
        invoice.counterpart.vatNumber.Should().Be("026883248");
        invoice.counterpart.country.Should().Be(CountryType.GR);
        invoice.counterpart.name.Should().BeNull();
        invoice.counterpart.address.Should().BeNull();
        
        // Verify invoice details
        invoice.invoiceDetails.Should().HaveCount(1);
        var invoiceDetail = invoice.invoiceDetails[0];
        invoiceDetail.netValue.Should().Be(8);
        invoiceDetail.vatAmount.Should().Be(0);
        invoiceDetail.quantity.Should().Be(1);
        invoiceDetail.lineNumber.Should().Be(1);
        
        // Verify VAT exemption category is set for 0% VAT
        invoiceDetail.vatCategory.Should().Be(fiskaltrust.Middleware.SCU.GR.MyData.Models.MyDataVatCategory.RegistrationsWithoutVat);
        invoiceDetail.vatExemptionCategorySpecified.Should().BeFalse();
        
        // Verify payment methods
        invoice.paymentMethods.Should().HaveCount(1);
        var paymentMethod = invoice.paymentMethods[0];
        paymentMethod.amount.Should().Be(8);
        paymentMethod.paymentMethodInfo.Should().Be("Cash");
        
        // Verify invoice summary
        invoice.invoiceSummary.totalNetValue.Should().Be(8);
        invoice.invoiceSummary.totalVatAmount.Should().Be(0);
        invoice.invoiceSummary.totalGrossValue.Should().Be(8);
        
        // Verify issuer information
        invoice.issuer.Should().NotBeNull();
        invoice.issuer.vatNumber.Should().Be("112545020");
        invoice.issuer.country.Should().Be(CountryType.GR);
        invoice.issuer.branch.Should().Be(0);
    }

    [Fact]
    public void MapToInvoicesDoc_ForVATExemptReceiptWithCashPayment_ShouldWork_AndReturnClassification()
    {
        var cbReceiptReference = "8-1752270167120";
        var receiptMoment = DateTime.Parse("2025-07-25T07:05:21.392Z").ToUniversalTime();
        var ftPosSystemId = Guid.Parse("7b5955e3-4944-4ff3-8df9-46166b70132a");
        var ftCashBoxID = Guid.Parse("31f3defc-275d-4b6e-9f3f-fa09d64c1bb4");

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = ftCashBoxID,
            cbTerminalID = "",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = ftPosSystemId,
            cbArea = "0",
            cbUser = "Chef",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Πελάτης A.E.",
                CustomerId = null,
                CustomerType = null,
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerZip = null,
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
                CustomerVATId = "026883248"
            },
            cbPreviousReceiptReference = "7-1752270167120",
            ftReceiptCase = (ReceiptCase) 0x4752200000000001,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 8,
                    Description = "Grilled Chicken Sandwich",
                    ProductNumber = "004",
                    Quantity = 1,
                    VATRate = 0,
                    ftChargeItemCase = (ChargeItemCase)0x4752200000001128,
                    Moment = DateTime.Parse("2025-07-25T07:05:21.392Z").ToUniversalTime(),
                    Position = 1,
                    VATAmount = 0,
                    Unit = "kg"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Moment = DateTime.Parse("2025-07-25T07:05:29.600Z").ToUniversalTime(),
                    Description = "Cash",
                    Amount = 8,
                    ftPayItemCase = (PayItemCase)0x4752200000000001
                }
            },
            cbReceiptAmount = 8
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "112545020"
            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Verify no error occurred
        error.Should().BeNull();

        // Verify invoice was created
        action.Should().NotBeNull();
        action!.invoice.Should().HaveCount(1);

        var invoice = action.invoice[0];

        // Verify basic invoice header
        invoice.invoiceHeader.series.Should().Be(receiptResponse.ftCashBoxIdentification);
        invoice.invoiceHeader.aa.Should().Be("291"); // This is based on the hexadecimal conversion
        invoice.invoiceHeader.issueDate.Should().Be(receiptMoment);
        invoice.invoiceHeader.currency.Should().Be(CurrencyType.EUR);

        // Verify invoice details
        invoice.invoiceDetails.Should().HaveCount(1);
        var invoiceDetail = invoice.invoiceDetails[0];
        invoiceDetail.netValue.Should().Be(8);
        invoiceDetail.vatAmount.Should().Be(0);
        invoiceDetail.quantity.Should().Be(1);
        invoiceDetail.lineNumber.Should().Be(1);

        // Verify VAT exemption category is set for 0% VAT
        invoiceDetail.vatCategory.Should().Be(fiskaltrust.Middleware.SCU.GR.MyData.Models.MyDataVatCategory.VatRate0_ExcludingVat_Category7);
        invoiceDetail.vatExemptionCategorySpecified.Should().BeTrue();

        // Verify payment methods
        invoice.paymentMethods.Should().HaveCount(1);
        var paymentMethod = invoice.paymentMethods[0];
        paymentMethod.amount.Should().Be(8);
        paymentMethod.paymentMethodInfo.Should().Be("Cash");

        // Verify invoice summary
        invoice.invoiceSummary.totalNetValue.Should().Be(8);
        invoice.invoiceSummary.totalVatAmount.Should().Be(0);
        invoice.invoiceSummary.totalGrossValue.Should().Be(8);

        // Verify issuer information
        invoice.issuer.Should().NotBeNull();
        invoice.issuer.vatNumber.Should().Be("112545020");
        invoice.issuer.country.Should().Be(CountryType.GR);
        invoice.issuer.branch.Should().Be(0);
    }

    [Fact]
    public void MapToInvoicesDoc_ForPaymentTransferCashPayment_ShouldWork_AndReturnClassification()
    {
        var cbReceiptReference = "8-1752270167120";
        var receiptMoment = DateTime.Parse("2025-07-25T07:05:21.392Z").ToUniversalTime();
        var ftPosSystemId = Guid.Parse("7b5955e3-4944-4ff3-8df9-46166b70132a");
        var ftCashBoxID = Guid.Parse("31f3defc-275d-4b6e-9f3f-fa09d64c1bb4");

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = ftCashBoxID,
            cbTerminalID = "",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = ftPosSystemId,
            cbArea = "0",
            cbUser = "Chef",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Πελάτης A.E.",
                CustomerId = null,
                CustomerType = null,
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerZip = null,
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
                CustomerVATId = "026883248"
            },
            cbPreviousReceiptReference = "7-1752270167120",
            ftReceiptCase = ((ReceiptCase) 0x4752200000000000).WithCase(ReceiptCase.PaymentTransfer0x0002),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 8,
                    Description = "Grilled Chicken Sandwich",
                    ProductNumber = "004",
                    Quantity = 1,
                    VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase)0x4752200000000000).WithTypeOfService(ChargeItemCaseTypeOfService.Receivable),
                    Moment = DateTime.Parse("2025-07-25T07:05:21.392Z").ToUniversalTime(),
                    Position = 1,
                    VATAmount = 0,
                    Unit = "kg"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Moment = DateTime.Parse("2025-07-25T07:05:29.600Z").ToUniversalTime(),
                    Description = "Cash",
                    Amount = 8,
                    ftPayItemCase = (PayItemCase)0x4752200000000001
                }
            },
            cbReceiptAmount = 8
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "112545020"
            },
        });

        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Verify no error occurred
        error.Should().BeNull();

        // Verify invoice was created
        action.Should().NotBeNull();
        action!.invoice.Should().HaveCount(1);

        var invoice = action.invoice[0];

        // Verify basic invoice header
        invoice.invoiceHeader.series.Should().Be(receiptResponse.ftCashBoxIdentification);
        invoice.invoiceHeader.aa.Should().Be("291"); // This is based on the hexadecimal conversion
        invoice.invoiceHeader.issueDate.Should().Be(receiptMoment);
        invoice.invoiceHeader.currency.Should().Be(CurrencyType.EUR);

        // Verify customer information
        invoice.counterpart.Should().NotBeNull();
        invoice.counterpart.vatNumber.Should().Be("026883248");
        invoice.counterpart.country.Should().Be(CountryType.GR);
        invoice.counterpart.name.Should().BeNull();
        invoice.counterpart.address.Should().BeNull();

        // Verify invoice details
        invoice.invoiceDetails.Should().HaveCount(1);
        var invoiceDetail = invoice.invoiceDetails[0];
        invoiceDetail.netValue.Should().Be(8);
        invoiceDetail.vatAmount.Should().Be(0);
        invoiceDetail.quantity.Should().Be(1);
        invoiceDetail.lineNumber.Should().Be(1);

        // Verify VAT exemption category is set for 0% VAT
        invoiceDetail.vatCategory.Should().Be(fiskaltrust.Middleware.SCU.GR.MyData.Models.MyDataVatCategory.RegistrationsWithoutVat);
        invoiceDetail.vatExemptionCategorySpecified.Should().BeFalse();

        // Verify payment methods
        invoice.paymentMethods.Should().HaveCount(1);
        var paymentMethod = invoice.paymentMethods[0];
        paymentMethod.amount.Should().Be(8);
        paymentMethod.paymentMethodInfo.Should().Be("Cash");

        // Verify invoice summary
        invoice.invoiceSummary.totalNetValue.Should().Be(8);
        invoice.invoiceSummary.totalVatAmount.Should().Be(0);
        invoice.invoiceSummary.totalGrossValue.Should().Be(8);

        // Verify issuer information
        invoice.issuer.Should().NotBeNull();
        invoice.issuer.vatNumber.Should().Be("112545020");
        invoice.issuer.country.Should().Be(CountryType.GR);
        invoice.issuer.branch.Should().Be(0);
    }

    [Fact]
    public void MapToInvoicesDoc_ForReceiptWithVATNature_ShouldIncludeIncomeClassifications()
    {
        var cbReceiptReference = "TEST-001";
        var receiptMoment = DateTime.Parse("2025-01-15T10:30:00.000Z").ToUniversalTime();
        var ftPosSystemId = Guid.Parse("7b5955e3-4944-4ff3-8df9-46166b70132a");
        var ftCashBoxID = Guid.Parse("31f3defc-275d-4b6e-9f3f-fa09d64c1bb4");

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = ftCashBoxID,
            cbTerminalID = "POS001",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = ftPosSystemId,
            cbArea = "Main",
            cbUser = "Cashier01",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Test Customer Ltd.",
                CustomerId = "CUST001",
                CustomerStreet = "Test Street 123",
                CustomerCity = "Athens",
                CustomerCountry = "GR",
                CustomerVATId = "123456789"
            },
            ftReceiptCase = (ReceiptCase)0x4752200000000001, // Point of sale receipt
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 124,
                    Description = "Standard Product with Normal VAT",
                    ProductNumber = "PROD001",
                    Quantity = 1,
                    VATRate = 24,
                    // Using standard VAT case (no exemption) - this should get income classification
                    ftChargeItemCase = (ChargeItemCase)0x4752200000000003, // Normal VAT + delivery service  
                    Moment = receiptMoment,
                    Position = 1,
                    VATAmount = 24,
                    Unit = "pcs"
                },
                new ChargeItem
                {
                    Amount = 100,
                    Description = "Another Standard Product",
                    ProductNumber = "PROD002", 
                    Quantity = 1,
                    VATRate = 24,
                    // Regular product with delivery service
                    ftChargeItemCase = (ChargeItemCase)0x4752200000000003, // Normal VAT + delivery service
                    Moment = receiptMoment,
                    Position = 2,
                    VATAmount = 24,
                    Unit = "pcs"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Moment = receiptMoment,
                    Description = "Cash Payment",
                    Amount = 224,
                    ftPayItemCase = (PayItemCase)0x4752200000000001
                }
            },
            cbReceiptAmount = 224
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "999123456"
            },
        });

        // Act
        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        action.Should().NotBeNull();
        action!.invoice.Should().HaveCount(1);

        var invoice = action.invoice[0];

        // Verify basic invoice structure
        invoice.invoiceHeader.Should().NotBeNull();
        invoice.invoiceDetails.Should().HaveCount(2);

        // Verify first charge item has income classification
        var firstDetail = invoice.invoiceDetails[0];
        firstDetail.lineNumber.Should().Be(1);
        firstDetail.netValue.Should().Be(100); // 124 - 24 VAT
        firstDetail.vatAmount.Should().Be(24);
        
        // Verify income classifications are present for normal VAT items
        firstDetail.incomeClassification.Should().NotBeNull();
        firstDetail.incomeClassification.Should().HaveCount(1);
        var incomeClassification1 = firstDetail.incomeClassification[0];
        incomeClassification1.amount.Should().Be(100);
        // Based on the actual mapping behavior - for unknown service types only category is set
        incomeClassification1.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        incomeClassification1.classificationTypeSpecified.Should().BeFalse(); // For unknown services

        // Verify second charge item has income classification
        var secondDetail = invoice.invoiceDetails[1];
        secondDetail.lineNumber.Should().Be(2);
        secondDetail.netValue.Should().Be(76); // 100 - 24 VAT  
        secondDetail.vatAmount.Should().Be(24);

        // Verify income classifications are present for standard item
        secondDetail.incomeClassification.Should().NotBeNull();
        secondDetail.incomeClassification.Should().HaveCount(1);
        var incomeClassification2 = secondDetail.incomeClassification[0];
        incomeClassification2.amount.Should().Be(76);
        // Based on the actual mapping behavior - for unknown service types only category is set
        incomeClassification2.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        incomeClassification2.classificationTypeSpecified.Should().BeFalse(); // For unknown services

        // Verify invoice summary includes income classifications
        invoice.invoiceSummary.Should().NotBeNull();
        invoice.invoiceSummary.incomeClassification.Should().NotBeNull();
        invoice.invoiceSummary.incomeClassification.Should().HaveCount(1); // Should be grouped by classification category

        // Verify summary income classifications are properly aggregated
        // For unknown services, they're grouped by category only (not by classification type)
        var summaryClassification = invoice.invoiceSummary.incomeClassification
            .FirstOrDefault(ic => ic.classificationCategory == IncomeClassificationCategoryType.category1_95);
        summaryClassification.Should().NotBeNull();
        summaryClassification!.amount.Should().Be(176); // 100 + 76 = 176 (sum of both items)
        summaryClassification.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        summaryClassification.classificationTypeSpecified.Should().BeFalse(); // For unknown services

        // Verify counterpart is null for receipt type invoices (InvoiceType.Item111 doesn't support counterpart)
        invoice.counterpart.Should().BeNull();

        // Verify issuer information
        invoice.issuer.Should().NotBeNull();
        invoice.issuer.vatNumber.Should().Be("999123456");
        invoice.issuer.country.Should().Be(CountryType.GR);
    }

    [Fact]
    public void MapToInvoicesDoc_ForReceiptWithVATNature_0014_ShouldIncludeIncomeClassifications()
    {
        var cbReceiptReference = "TEST-001";
        var receiptMoment = DateTime.Parse("2025-01-15T10:30:00.000Z").ToUniversalTime();
        var ftPosSystemId = Guid.Parse("7b5955e3-4944-4ff3-8df9-46166b70132a");
        var ftCashBoxID = Guid.Parse("31f3defc-275d-4b6e-9f3f-fa09d64c1bb4");

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = ftCashBoxID,
            cbTerminalID = "POS001",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = ftPosSystemId,
            cbArea = "Main",
            cbUser = "Cashier01",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Test Customer Ltd.",
                CustomerId = "CUST001",
                CustomerStreet = "Test Street 123",
                CustomerCity = "Athens",
                CustomerCountry = "GR",
                CustomerVATId = "123456789"
            },
            ftReceiptCase = (ReceiptCase) 0x4752200000000001, // Point of sale receipt
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 124,
                    Description = "Standard Product with Normal VAT",
                    ProductNumber = "PROD001",
                    Quantity = 1,
                    VATRate = 0,
                    ftChargeItemCase = (ChargeItemCase)0x4752200000001417,
                    Moment = receiptMoment,
                    Position = 1,
                    VATAmount = 0,
                    Unit = "pcs"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Moment = receiptMoment,
                    Description = "Cash Payment",
                    Amount = 124,
                    ftPayItemCase = (PayItemCase)0x4752200000000001
                }
            },
            cbReceiptAmount = 124
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "999123456"
            },
        });

        // Act
        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        action.Should().NotBeNull();
        action!.invoice.Should().HaveCount(1);

        var invoice = action.invoice[0];

        // Verify basic invoice structure
        invoice.invoiceHeader.Should().NotBeNull();
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111); // Invoice type

        invoice.invoiceDetails.Should().HaveCount(1);

        // Verify first charge item has income classification
        var firstDetail = invoice.invoiceDetails[0];
        firstDetail.lineNumber.Should().Be(1);
        firstDetail.netValue.Should().Be(124);
        firstDetail.vatAmount.Should().Be(0);

        // Verify income classifications are present for normal VAT items
        firstDetail.incomeClassification.Should().NotBeNull();
        firstDetail.incomeClassification.Should().HaveCount(1);
        var incomeClassification1 = firstDetail.incomeClassification[0];
        incomeClassification1.amount.Should().Be(124);
        // Based on the actual mapping behavior - for unknown service types only category is set
        incomeClassification1.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1);
        
        incomeClassification1.classificationTypeSpecified.Should().BeTrue(); // For unknown services
        incomeClassification1.classificationType.Should().Be(IncomeClassificationValueType.E3_561_004);

        // Verify invoice summary includes income classifications
        invoice.invoiceSummary.Should().NotBeNull();
        invoice.invoiceSummary.incomeClassification.Should().NotBeNull();
        invoice.invoiceSummary.incomeClassification.Should().HaveCount(1); // Should be grouped by classification category

        // Verify counterpart is null for receipt type invoices (InvoiceType.Item111 doesn't support counterpart)
        invoice.counterpart.Should().BeNull();

        // Verify issuer information
        invoice.issuer.Should().NotBeNull();
        invoice.issuer.vatNumber.Should().Be("999123456");
        invoice.issuer.country.Should().Be(CountryType.GR);
    }

    [Fact]
    public void MapToInvoicesDoc_ForInvoiceWithVATNature_0014_ShouldIncludeIncomeClassifications()
    {
        var cbReceiptReference = "TEST-001";
        var receiptMoment = DateTime.Parse("2025-01-15T10:30:00.000Z").ToUniversalTime();
        var ftPosSystemId = Guid.Parse("7b5955e3-4944-4ff3-8df9-46166b70132a");
        var ftCashBoxID = Guid.Parse("31f3defc-275d-4b6e-9f3f-fa09d64c1bb4");

        var receiptRequest = new ReceiptRequest
        {
            ftCashBoxID = ftCashBoxID,
            cbTerminalID = "POS001",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = ftPosSystemId,
            cbArea = "Main",
            cbUser = "Cashier01",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Test Customer Ltd.",
                CustomerId = "CUST001",
                CustomerStreet = "Test Street 123",
                CustomerCity = "Athens",
                CustomerCountry = "GR",
                CustomerVATId = "123456789"
            },
            ftReceiptCase = (ReceiptCase) 0x4752200000001002, // Point of sale receipt
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 124,
                    Description = "Standard Product with Normal VAT",
                    ProductNumber = "PROD001",
                    Quantity = 1,
                    VATRate = 0,
                    ftChargeItemCase = (ChargeItemCase)0x4752200000001417,
                    Moment = receiptMoment,
                    Position = 1,
                    VATAmount = 0,
                    Unit = "pcs"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Moment = receiptMoment,
                    Description = "Cash Payment",
                    Amount = 124,
                    ftPayItemCase = (PayItemCase)0x4752200000000001
                }
            },
            cbReceiptAmount = 124
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "999123456"
            },
        });

        // Act
        (var action, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        action.Should().NotBeNull();
        action!.invoice.Should().HaveCount(1);

        var invoice = action.invoice[0];
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item11); // Invoice type

        // Verify basic invoice structure
        invoice.invoiceHeader.Should().NotBeNull();
        invoice.invoiceDetails.Should().HaveCount(1);

        // Verify first charge item has income classification
        var firstDetail = invoice.invoiceDetails[0];
        firstDetail.lineNumber.Should().Be(1);
        firstDetail.netValue.Should().Be(124);
        firstDetail.vatAmount.Should().Be(0);

        // Verify income classifications are present for normal VAT items
        firstDetail.incomeClassification.Should().NotBeNull();
        firstDetail.incomeClassification.Should().HaveCount(1);
        var incomeClassification1 = firstDetail.incomeClassification[0];
        incomeClassification1.amount.Should().Be(124);
        // Based on the actual mapping behavior - for unknown service types only category is set
        incomeClassification1.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1);
        
        incomeClassification1.classificationTypeSpecified.Should().BeTrue(); // For unknown services
        incomeClassification1.classificationType.Should().Be(IncomeClassificationValueType.E3_561_002);

        invoice.counterpart.Should().NotBeNull();
        invoice.counterpart.vatNumber.Should().Be("123456789");
        invoice.counterpart.country.Should().Be(CountryType.GR);
        invoice.counterpart.name.Should().BeNull();
        invoice.counterpart.address.Should().BeNull();

        // Verify invoice summary includes income classifications
        invoice.invoiceSummary.Should().NotBeNull();
        invoice.invoiceSummary.incomeClassification.Should().NotBeNull();
        invoice.invoiceSummary.incomeClassification.Should().HaveCount(1); // Should be grouped by classification category

        // Verify issuer information
        invoice.issuer.Should().NotBeNull();
        invoice.issuer.vatNumber.Should().Be("999123456");
        invoice.issuer.country.Should().Be(CountryType.GR);
    }

    [Fact]
    public void MapToInvoicesDoc_WithHasTransportInformationFlag_ShouldSetIsDeliveryNoteToTrue()
    {
        // Arrange
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                Description = "Test Item",
                Quantity = 1,
                Unit = "Kg",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100,
                    ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
                .WithFlag(ReceiptCaseFlagsGR.HasTransportInformation),
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "TEST-123"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789",
                AccountName = "Test Company"
            },
            Outlet = new storage.V0.MasterData.OutletMasterData
            {
                LocationId = "1",
                Street = "Test Street",
                City = "Athens",
                Zip = "12345"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        invoiceDoc!.invoice.Should().HaveCount(1);
        
        var invoice = invoiceDoc.invoice[0];
        invoice.invoiceHeader.Should().NotBeNull();
        invoice.invoiceHeader.isDeliveryNote.Should().BeTrue("HasTransportInformation flag should set isDeliveryNote to true");
        invoice.invoiceHeader.isDeliveryNoteSpecified.Should().BeTrue("isDeliveryNoteSpecified should be set to true");
    }

    [Fact]
    public void MapToInvoicesDoc_WithoutHasTransportInformationFlag_ShouldNotSetIsDeliveryNote()
    {
        // Arrange
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                Description = "Test Item",
                Quantity = 1,
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        };

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100,
                    ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            ],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "TEST-123"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789",
                AccountName = "Test Company"
            },
            Outlet = new storage.V0.MasterData.OutletMasterData
            {
                LocationId = "1",
                Street = "Test Street",
                City = "Athens",
                Zip = "12345"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        invoiceDoc!.invoice.Should().HaveCount(1);
        
        var invoice = invoiceDoc.invoice[0];
        invoice.invoiceHeader.Should().NotBeNull();
        invoice.invoiceHeader.isDeliveryNoteSpecified.Should().BeFalse("isDeliveryNoteSpecified should not be set when flag is not present");
    }
    [Fact]
    public void MapToInvoicesDoc_B2BInvoice_WithPreviousReference_UsesCorrelatedInvoices()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = -100,
                    ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATRate = 24
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = -100 }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.InvoiceB2B0x1002)
                .WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "PREV-B2B-123",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "123456789",
                CustomerCountry = "GR"
            }
        };
        
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123ABC#",
            ftCashBoxIdentification = "CB-TEST",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem { Caption = "invoiceMark", Data = "400001958034189" }
            }
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData()
        });

        // Act
        (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, new List<(ReceiptRequest, ReceiptResponse)>
        {
            (receiptRequest, receiptResponse)
        });

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        
        // B2B invoices should use correlatedInvoices, NOT multipleConnectedMarks
        header.correlatedInvoices.Should().NotBeNull();
        header.correlatedInvoices.Should().Contain(400001958034189);
        header.multipleConnectedMarks.Should().BeNull();
    }
    [Fact]
    public void MapToInvoicesDoc_B2BInvoice_11_NonRefund_WithPreviousReference_SetsMultipleConnectedMarks()
    {
        // Arrange - B2B Invoice (Type 1.1) non-refund with previous reference
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 100 }
        },
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbPreviousReceiptReference = "PREV-B2B-123",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "123456789",
                CustomerCountry = "GR" // Domestic customer
            }
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123ABC#",
            ftCashBoxIdentification = "CB-TEST",
            ftSignatures = new List<SignatureItem>
        {
            new SignatureItem { Caption = "invoiceMark", Data = "400001958034189" }
        }
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData()
        });

        // Act
        (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, new List<(ReceiptRequest, ReceiptResponse)>
    {
        (receiptRequest, receiptResponse)
    });

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;

        // Invoice 1.1 non-refund with previous reference SHOULD use multipleConnectedMarks
        header.invoiceType.Should().Be(InvoiceType.Item11);
        header.multipleConnectedMarks.Should().NotBeNull();
        header.multipleConnectedMarks.Should().Contain(400001958034189);
        header.correlatedInvoices.Should().BeNull();
    }
    [Fact]
    public void MapToInvoicesDoc_B2BInvoice_12_NonRefund_WithPreviousReference_SetsMultipleConnectedMarks()
    {
        // Arrange - B2B Invoice (Type 1.2) non-refund with previous reference to EU customer
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 100 }
        },
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbPreviousReceiptReference = "PREV-B2B-EU-123",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "DE123456789",
                CustomerCountry = "DE" // EU customer generates 1.2
            }
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123DEF#",
            ftCashBoxIdentification = "CB-TEST",
            ftSignatures = new List<SignatureItem>
        {
            new SignatureItem { Caption = "invoiceMark", Data = "400002958034190" }
        }
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData()
        });

        // Act
        (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, new List<(ReceiptRequest, ReceiptResponse)>
    {
        (receiptRequest, receiptResponse)
    });

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;

        // Invoice 1.2 non-refund with previous reference SHOULD use multipleConnectedMarks
        header.invoiceType.Should().Be(InvoiceType.Item12);
        header.multipleConnectedMarks.Should().NotBeNull();
        header.multipleConnectedMarks.Should().Contain(400002958034190);
        header.correlatedInvoices.Should().BeNull();
    }
    [Fact]
    public void MapToInvoicesDoc_RetailCreditNote_WithPreviousReference_SetsMultipleConnectedMarks()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = -10,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = -10
            }
        },
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
                .WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "400001958034189"
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-TEST",
            ftSignatures = new List<SignatureItem>
        {
            new SignatureItem { Caption = "invoiceMark", Data = "400001958034189" }
        }
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData()
        });

        // Act
        (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, new List<(ReceiptRequest, ReceiptResponse)>
    {
        (receiptRequest, receiptResponse)
    });

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().NotBeNull();
        header.multipleConnectedMarks.Should().Contain(400001958034189);
    }
    [Fact]
    public void MapToInvoicesDoc_RetailCreditNote_WithoutReference_DoesNotSetMultipleConnectedMarks()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = -10,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = -10
            }
        },
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
                .WithFlag(ReceiptCaseFlags.Refund)
            // No cbPreviousReceiptReference
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-TEST"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData()
        });

        // Act
        (var doc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeNull();
    }
    [Fact]
    public void MapToInvoicesDoc_WithTipAsChargeItem_ShouldExcludeTipFromMyDataPayload()
    {
        // Arrange
        var receiptMoment = DateTime.Parse("2025-01-24T10:00:00.000Z").ToUniversalTime();
        var cbReceiptReference = Guid.NewGuid().ToString();

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 124,
                Description = "Food",
                Quantity = 1,
                VATRate = 24,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                Position = 1,
                VATAmount = 24
            },
            new ChargeItem
            {
                Amount = 4,
                Description = "Tip",
                Quantity = 1,
                VATRate = 0,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithTypeOfService(ChargeItemCaseTypeOfService.Tip),
                Position = 2,
                VATAmount = 0
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = 128, // 124 Food + 4 Tip
                Description = "Card Payment",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 1
            }
        },
            cbReceiptAmount = 128
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var invoice = invoiceDoc!.invoice[0];

        invoice.invoiceDetails.Should().HaveCount(1, "tips should be excluded from invoice details");

        invoice.invoiceDetails[0].lineNumber.Should().Be(1);
        invoice.invoiceDetails[0].netValue.Should().Be(100);
        invoice.invoiceDetails[0].vatAmount.Should().Be(24);

        invoice.invoiceSummary.totalNetValue.Should().Be(100, "tip should not be included in net value");
        invoice.invoiceSummary.totalVatAmount.Should().Be(24, "tip should not be included in VAT");
        invoice.invoiceSummary.totalGrossValue.Should().Be(124, "tip should not be included in gross value");

        invoice.paymentMethods.Should().HaveCount(1);
        invoice.paymentMethods[0].amount.Should().Be(124, "payment reported to myDATA must exclude tip");
        invoice.paymentMethods[0].tipAmountSpecified.Should().BeFalse("tip field must be null/false in XML");
    }
    [Fact]
    public void MapToInvoicesDoc_WithTipAsPayItem_ShouldExcludeTipFromMyDataPayload()
    {
        // Arrange  
        var receiptMoment = DateTime.Parse("2025-01-24T10:00:00.000Z").ToUniversalTime();
        var cbReceiptReference = Guid.NewGuid().ToString();

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 124,
                Description = "Food",
                Quantity = 1,
                VATRate = 24,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                Position = 1,
                VATAmount = 24
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = 124,
                Description = "Card Payment",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 1
            },
            new PayItem
            {
                Amount = 4,
                Description = "Tip",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000)
                    .WithCase(PayItemCase.DebitCardPayment)
                    .WithFlag(PayItemCaseFlags.Tip),
                Position = 2
            }
        },
            cbReceiptAmount = 128 // Total 124 + 4
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var invoice = invoiceDoc!.invoice[0];

        invoice.invoiceSummary.totalGrossValue.Should().Be(124);

        var totalReportedPayment = invoice.paymentMethods.Sum(p => p.amount);
        totalReportedPayment.Should().Be(124, "only non-tip payment amounts should be sent to myDATA");

        foreach (var payment in invoice.paymentMethods)
        {
            payment.tipAmountSpecified.Should().BeFalse("tips must not be sent to myDATA");
        }
    }
    [Fact]
    public void MapToInvoicesDoc_WithTipAsChargeItem_MultiplePaymentMethods_ShouldDistributeTipProportionally()
    {
        // Arrange -
        var receiptMoment = DateTime.Parse("2025-01-24T10:00:00.000Z").ToUniversalTime();
        var cbReceiptReference = Guid.NewGuid().ToString();

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 300,
                Description = "Food & Drinks",
                Quantity = 1,
                VATRate = 24,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                Position = 1,
                VATAmount = 58.06m
            },
            new ChargeItem
            {
                Amount = 30,
                Description = "Tip",
                Quantity = 1,
                VATRate = 0,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithTypeOfService(ChargeItemCaseTypeOfService.Tip),
                Position = 2,
                VATAmount = 0
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = 200,
                Description = "Card Payment",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 1
            },
            new PayItem
            {
                Amount = 130,
                Description = "Cash Payment",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
                Position = 2
            }
        },
            cbReceiptAmount = 330 // 300 (bill) + 30 (tip)
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var invoice = invoiceDoc!.invoice[0];

        invoice.invoiceDetails.Should().HaveCount(1, "tips should be excluded from invoice details");
        invoice.invoiceDetails[0].lineNumber.Should().Be(1);
        invoice.invoiceDetails[0].netValue.Should().Be(241.94m);
        invoice.invoiceDetails[0].vatAmount.Should().Be(58.06m);

        invoice.invoiceSummary.totalNetValue.Should().Be(241.94m, "tip should not be included in net value");
        invoice.invoiceSummary.totalVatAmount.Should().Be(58.06m, "tip should not be included in VAT");
        invoice.invoiceSummary.totalGrossValue.Should().Be(300, "tip should not be included in gross value");

        invoice.paymentMethods.Should().HaveCount(2, "both payment methods should be reported");

        var cardPayment = invoice.paymentMethods.FirstOrDefault(p => p.paymentMethodInfo == "Card Payment");
        cardPayment.Should().NotBeNull();
        cardPayment!.amount.Should().Be(181.82m, "card payment: 200 - (30 × 200/330) ≈ 181.82");

        var cashPayment = invoice.paymentMethods.FirstOrDefault(p => p.paymentMethodInfo == "Cash Payment");
        cashPayment.Should().NotBeNull();
        cashPayment!.amount.Should().Be(118.18m, "cash payment: 130 - (30 × 130/330) ≈ 118.18");

        var totalReportedPayment = invoice.paymentMethods.Sum(p => p.amount);
        totalReportedPayment.Should().Be(300m, "total payments should match invoice gross value (tip excluded)");

        foreach (var payment in invoice.paymentMethods)
        {
            payment.tipAmountSpecified.Should().BeFalse("tip field must be null/false in XML");
        }
    }
    [Fact]
    public void MapToInvoicesDoc_WithTipLargerThanAnyIndividualPayment_ShouldDistributeTipProportionally()
    {
        // Arrange
        var receiptMoment = DateTime.Parse("2025-01-24T10:00:00.000Z").ToUniversalTime();
        var cbReceiptReference = Guid.NewGuid().ToString();

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 30,
                Description = "Coffee",
                Quantity = 1,
                VATRate = 24,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                Position = 1,
                VATAmount = 5.81m
            },
            new ChargeItem
            {
                Amount = 30,
                Description = "Tip",
                Quantity = 1,
                VATRate = 0,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithTypeOfService(ChargeItemCaseTypeOfService.Tip),
                Position = 2,
                VATAmount = 0
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = 20,
                Description = "Card Payment 1",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 1
            },
            new PayItem
            {
                Amount = 20,
                Description = "Card Payment 2",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 2
            },
            new PayItem
            {
                Amount = 20,
                Description = "Cash Payment",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
                Position = 3
            }
        },
            cbReceiptAmount = 60 // 30 (bill) + 30 (tip)
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var invoice = invoiceDoc!.invoice[0];

        invoice.invoiceDetails.Should().HaveCount(1, "tips should be excluded from invoice details");
        invoice.invoiceDetails[0].netValue.Should().Be(24.19m);
        invoice.invoiceDetails[0].vatAmount.Should().Be(5.81m);

        invoice.invoiceSummary.totalNetValue.Should().Be(24.19m, "tip should not be included in net value");
        invoice.invoiceSummary.totalVatAmount.Should().Be(5.81m, "tip should not be included in VAT");
        invoice.invoiceSummary.totalGrossValue.Should().Be(30, "tip should not be included in gross value");

        invoice.paymentMethods.Should().HaveCount(3, "all three payment methods should be reported");

        var cardPayment1 = invoice.paymentMethods.FirstOrDefault(p => p.paymentMethodInfo == "Card Payment 1");
        cardPayment1.Should().NotBeNull();
        cardPayment1!.amount.Should().Be(10m, "card payment 1: 20 - (30 × 20/60) = 10");

        var cardPayment2 = invoice.paymentMethods.FirstOrDefault(p => p.paymentMethodInfo == "Card Payment 2");
        cardPayment2.Should().NotBeNull();
        cardPayment2!.amount.Should().Be(10m, "card payment 2: 20 - (30 × 20/60) = 10");

        var cashPayment = invoice.paymentMethods.FirstOrDefault(p => p.paymentMethodInfo == "Cash Payment");
        cashPayment.Should().NotBeNull();
        cashPayment!.amount.Should().Be(10m, "cash payment: 20 - (30 × 20/60) = 10");

        var totalReportedPayment = invoice.paymentMethods.Sum(p => p.amount);
        totalReportedPayment.Should().Be(30m, "total payments should match invoice gross value (tip excluded)");

        foreach (var payment in invoice.paymentMethods)
        {
            payment.tipAmountSpecified.Should().BeFalse("tip field must be null/false in XML");
        }
    }
    [Fact]
    public void MapToInvoicesDoc_WithTipDistribution_ShouldHandleRoundingRemainder()
    {
        // Arrange
        var receiptMoment = DateTime.Parse("2025-01-24T10:00:00.000Z").ToUniversalTime();
        var cbReceiptReference = Guid.NewGuid().ToString();

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = receiptMoment,
            cbReceiptReference = cbReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 29,
                Description = "Meal",
                Quantity = 1,
                VATRate = 24,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                Position = 1,
                VATAmount = 5.61m
            },
            new ChargeItem
            {
                Amount = 1,
                Description = "Tip",
                Quantity = 1,
                VATRate = 0,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000)
                    .WithTypeOfService(ChargeItemCaseTypeOfService.Tip),
                Position = 2,
                VATAmount = 0
            }
        },
            cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                Amount = 10,
                Description = "Card Payment 1",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 1
            },
            new PayItem
            {
                Amount = 10,
                Description = "Card Payment 2",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment),
                Position = 2
            },
            new PayItem
            {
                Amount = 10,
                Description = "Cash Payment",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
                Position = 3
            }
        },
            cbReceiptAmount = 30 // 29 (bill) + 1 (tip)
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "CB-001"
        };

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "123456789"
            }
        });

        // Act
        (var invoiceDoc, var error) = aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);

        // Assert
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var invoice = invoiceDoc!.invoice[0];

        invoice.invoiceDetails.Should().HaveCount(1, "tips should be excluded from invoice details");
        invoice.invoiceDetails[0].netValue.Should().Be(23.39m);
        invoice.invoiceDetails[0].vatAmount.Should().Be(5.61m);

        invoice.invoiceSummary.totalNetValue.Should().Be(23.39m);
        invoice.invoiceSummary.totalVatAmount.Should().Be(5.61m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(29m);

        invoice.paymentMethods.Should().HaveCount(3, "all three payment methods should be reported");

        var payments = invoice.paymentMethods.OrderBy(p => p.paymentMethodInfo).ToList();
        var payment966 = payments.Where(p => p.amount == 9.66m).ToList();
        var payment967 = payments.Where(p => p.amount == 9.67m).ToList();

        payment966.Should().HaveCount(1, "exactly one payment should receive the -1 cent remainder correction");
        payment967.Should().HaveCount(2, "the other two payments should remain at 9.67");

        var totalReportedPayment = invoice.paymentMethods.Sum(p => p.amount);
        totalReportedPayment.Should().Be(29.00m, "remainder handling ensures exact total match - this is critical for MyData validation!");

        var sum = 9.66m + 9.67m + 9.67m;
        sum.Should().Be(29.00m, "manual verification: 9.66 + 9.67 + 9.67 = 29.00");

        foreach (var payment in invoice.paymentMethods)
        {
            payment.tipAmountSpecified.Should().BeFalse("tip field must be null/false in XML");
        }
    }
}
