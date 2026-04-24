using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class MyDataOverrideTests
{
    private AADEFactory CreateFactory()
    {
        return new AADEFactory(new MasterDataConfiguration
        {
            Account = new AccountMasterData
            {
                VatId = "123456789"
            },
            Outlet = new OutletMasterData
            {
                LocationId = "0"
            }
        }, "https://receipts.example.com");
    }

    private ReceiptRequest CreateBasicReceiptRequest()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2025, 6, 18, 10, 44, 19, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 100,
                    Quantity = 1,
                    Description = "Test Item",
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATRate = 24
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000
                }
            },
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        };
    }

    private ReceiptResponse CreateBasicReceiptResponse(ReceiptRequest request)
    {
        return new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftReceiptIdentification = "ft123ABC#",
            ftCashBoxIdentification = "TEST-001"
        };
    }

    [Fact]
    public void MapToInvoicesDoc_WithoutOverride_ShouldNotHaveDispatchFields()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.dispatchDateSpecified.Should().BeFalse();
        doc.invoice[0].invoiceHeader.dispatchTimeSpecified.Should().BeFalse();
        doc.invoice[0].invoiceHeader.movePurposeSpecified.Should().BeFalse();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithDispatchDateOverride_ShouldSetDispatchDate()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        var expectedDate = new DateTime(2025, 6, 18);
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            dispatchDate = "2025-06-18"
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.dispatchDateSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.dispatchDate.Date.Should().Be(expectedDate.Date);
    }

    [Fact]
    public void MapToInvoicesDoc_WithDispatchTimeOverride_ShouldSetDispatchTime()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            dispatchTime = new DateTime(1, 1, 1, 10, 44, 19).ToString("o")
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.dispatchTimeSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.dispatchTime.Hour.Should().Be(10);
        doc.invoice[0].invoiceHeader.dispatchTime.Minute.Should().Be(44);
        doc.invoice[0].invoiceHeader.dispatchTime.Second.Should().Be(19);
    }

    [Fact]
    public void MapToInvoicesDoc_WithMovePurposeOverride_ShouldSetMovePurpose()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            movePurpose = 1
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.movePurposeSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.movePurpose.Should().Be(1);
    }

    [Fact]
    public void MapToInvoicesDoc_WithLoadingAddressOverride_ShouldSetLoadingAddress()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            otherDeliveryNoteHeader = new
                            {
                                loadingAddress = new
                                {
                                    street = "Παπαδιαμάντη 24",
                                    number = "0",
                                    postalCode = "56429",
                                    city = "Νέα Ευκαρπία - Θεσσαλονίκη"
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.otherDeliveryNoteHeader.Should().NotBeNull();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.Should().NotBeNull();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("Παπαδιαμάντη 24");
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.number.Should().Be("0");
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.postalCode.Should().Be("56429");
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.city.Should().Be("Νέα Ευκαρπία - Θεσσαλονίκη");
    }

    [Fact]
    public void MapToInvoicesDoc_WithDeliveryAddressOverride_ShouldSetDeliveryAddress()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            otherDeliveryNoteHeader = new
                            {
                                deliveryAddress = new
                                {
                                    street = "ΙΚΤΙΝΟΥ 22",
                                    number = "0",
                                    postalCode = "54622",
                                    city = "ΘΕΣΣΑΛΟΝΙΚΗ"
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.otherDeliveryNoteHeader.Should().NotBeNull();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.Should().NotBeNull();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.street.Should().Be("ΙΚΤΙΝΟΥ 22");
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.number.Should().Be("0");
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.postalCode.Should().Be("54622");
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.city.Should().Be("ΘΕΣΣΑΛΟΝΙΚΗ");
    }

    [Fact]
    public void MapToInvoicesDoc_WithShippingBranchOverrides_ShouldSetShippingBranches()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            otherDeliveryNoteHeader = new
                            {
                                startShippingBranch = 5,
                                completeShippingBranch = 10
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.otherDeliveryNoteHeader.Should().NotBeNull();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.startShippingBranchSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.startShippingBranch.Should().Be(5);
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.completeShippingBranchSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.completeShippingBranch.Should().Be(10);
    }

    [Fact]
    public void MapToInvoicesDoc_WithCompleteOverride_ShouldSetAllFields()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            dispatchDate = new DateTime(2025, 06, 18, 10, 44, 19).ToString("o"),
                            dispatchTime = new DateTime(2025,06,18, 10, 44, 19).ToString("o"),
                            movePurpose = 1,
                            otherDeliveryNoteHeader = new
                            {
                                loadingAddress = new
                                {
                                    street = "Παπαδιαμάντη 24",
                                    number = "0",
                                    postalCode = "56429",
                                    city = "Νέα Ευκαρπία - Θεσσαλονίκη"
                                },
                                deliveryAddress = new
                                {
                                    street = "ΙΚΤΙΝΟΥ 22",
                                    number = "0",
                                    postalCode = "54622",
                                    city = "ΘΕΣΣΑΛΟΝΙΚΗ"
                                },
                                startShippingBranch = 0,
                                completeShippingBranch = 0
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        
        var invoice = doc!.invoice[0];
        
        // Verify dispatch date
        invoice.invoiceHeader.dispatchDateSpecified.Should().BeTrue();
        invoice.invoiceHeader.dispatchDate.Date.Should().Be(new DateTime(2025, 6, 18).Date);
        
        // Verify dispatch time
        invoice.invoiceHeader.dispatchTimeSpecified.Should().BeTrue();
        invoice.invoiceHeader.dispatchTime.Hour.Should().Be(10);
        invoice.invoiceHeader.dispatchTime.Minute.Should().Be(44);
        invoice.invoiceHeader.dispatchTime.Second.Should().Be(19);
        
        // Verify move purpose
        invoice.invoiceHeader.movePurposeSpecified.Should().BeTrue();
        invoice.invoiceHeader.movePurpose.Should().Be(1);
        
        // Verify delivery note header
        invoice.invoiceHeader.otherDeliveryNoteHeader.Should().NotBeNull();
        
        // Verify loading address
        invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.Should().NotBeNull();
        invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("Παπαδιαμάντη 24");
        invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.number.Should().Be("0");
        invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.postalCode.Should().Be("56429");
        invoice.invoiceHeader.otherDeliveryNoteHeader.loadingAddress.city.Should().Be("Νέα Ευκαρπία - Θεσσαλονίκη");
        
        // Verify delivery address
        invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.Should().NotBeNull();
        invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.street.Should().Be("ΙΚΤΙΝΟΥ 22");
        invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.number.Should().Be("0");
        invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.postalCode.Should().Be("54622");
        invoice.invoiceHeader.otherDeliveryNoteHeader.deliveryAddress.city.Should().Be("ΘΕΣΣΑΛΟΝΙΚΗ");
        
        // Verify shipping branches
        invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranchSpecified.Should().BeTrue();
        invoice.invoiceHeader.otherDeliveryNoteHeader.startShippingBranch.Should().Be(0);
        invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranchSpecified.Should().BeTrue();
        invoice.invoiceHeader.otherDeliveryNoteHeader.completeShippingBranch.Should().Be(0);
    }

    [Fact]
    public void MapToInvoicesDoc_WithPartialOverride_ShouldOnlySetSpecifiedFields()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            dispatchDate = "2025-06-18",
                            movePurpose = 1
                            // Note: dispatchTime, addresses, and branches are NOT specified
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        
        var invoice = doc!.invoice[0];
        
        // Verify specified fields are set
        invoice.invoiceHeader.dispatchDateSpecified.Should().BeTrue();
        invoice.invoiceHeader.movePurposeSpecified.Should().BeTrue();
        invoice.invoiceHeader.movePurpose.Should().Be(1);
        
        // Verify unspecified fields are not set
        invoice.invoiceHeader.dispatchTimeSpecified.Should().BeFalse();
        invoice.invoiceHeader.otherDeliveryNoteHeader.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithAddressNumberNull_ShouldDefaultToZero()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            otherDeliveryNoteHeader = new
                            {
                                loadingAddress = new
                                {
                                    street = "Test Street",
                                    postalCode = "12345",
                                    city = "Test City"
                                    // number is not provided (null)
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.number.Should().Be("0");
    }

    [Fact]
    public void MapToInvoicesDoc_WithOverride_ShouldSerializeToValidXml()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            dispatchDate = new DateTime(2025, 06, 18, 10, 44, 19).ToString("o"),
                            dispatchTime = new DateTime(2025, 06, 18, 10, 44, 19).ToString("o"),
                            movePurpose = 1,
                            otherDeliveryNoteHeader = new
                            {
                                loadingAddress = new
                                {
                                    street = "Παπαδιαμάντη 24",
                                    number = "0",
                                    postalCode = "56429",
                                    city = "Νέα Ευκαρπία"
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);
        var xml = AADEFactory.GenerateInvoicePayload(doc!);

        // Assert
        error.Should().BeNull();
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("dispatchDate");
        xml.Should().Contain("dispatchTime");
        xml.Should().Contain("movePurpose");
        xml.Should().Contain("loadingAddress");
        xml.Should().Contain("Παπαδιαμάντη 24");
        
        // Verify XML is valid by deserializing it
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        using var stringReader = new StringReader(xml);
        var deserializedDoc = (InvoicesDoc)xmlSerializer.Deserialize(stringReader)!;
        deserializedDoc.Should().NotBeNull();
        deserializedDoc.invoice[0].invoiceHeader.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("Παπαδιαμάντη 24");
    }

    [Fact]
    public void MapToInvoicesDoc_WithEmptyOverride_ShouldNotThrowException()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new { }
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithNullOverride_ShouldNotThrowException()
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = (object?)null
            }
        };

        var response = CreateBasicReceiptResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithReceiptBaseAddress_ShouldSetDownloadingInvoiceUrl()
    {
        // Arrange
        var factory = new AADEFactory(
            new MasterDataConfiguration
            {
                Account = new AccountMasterData { VatId = "123456789" },
                Outlet = new OutletMasterData { LocationId = "0" }
            },
            "https://receipts.example.com");

        var request = CreateBasicReceiptRequest();
        var response = CreateBasicReceiptResponse(request);
        response.ftQueueID = Guid.Parse("12345678-1234-1234-1234-123456789012");
        response.ftQueueItemID = Guid.Parse("87654321-4321-4321-4321-210987654321");

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].downloadingInvoiceUrl.Should().Be("https://receipts.example.com/12345678-1234-1234-1234-123456789012/87654321-4321-4321-4321-210987654321");
    }

    // === NEW HEADER OVERRIDE TESTS ===

    [Fact]
    public void MapToInvoicesDoc_WithExchangeRateOverride_ShouldSetExchangeRate()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            exchangeRate = 1.15m
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceHeader.exchangeRateSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.exchangeRate.Should().Be(1.15m);
    }

    [Fact]
    public void MapToInvoicesDoc_WithThirdPartyCollectionOverride_ShouldSetThirdPartyCollection()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            thirdPartyCollection = true
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceHeader.thirdPartyCollectionSpecified.Should().BeTrue();
        doc.invoice[0].invoiceHeader.thirdPartyCollection.Should().BeTrue();
    }


    // === COUNTERPART UNMAPPED FIELDS OVERRIDE TESTS ===

    [Fact]
    public void MapToInvoicesDoc_WithCounterpartDocumentIdOverride_ShouldSetDocumentIdNo()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        // Need a B2B receipt with customer so counterpart is created
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002);
        request.cbCustomer = new { CustomerVATId = "EL123456789", CustomerCountry = "GR" };
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        counterpart = new
                        {
                            documentIdNo = "AB123456",
                            supplyAccountNo = "SUP-001",
                            countryDocumentId = "GR"
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].counterpart.Should().NotBeNull();
        doc.invoice[0].counterpart.documentIdNo.Should().Be("AB123456");
        doc.invoice[0].counterpart.supplyAccountNo.Should().Be("SUP-001");
        doc.invoice[0].counterpart.countryDocumentIdSpecified.Should().BeTrue();
        doc.invoice[0].counterpart.countryDocumentId.Should().Be(CountryType.GR);
    }


    // === LINE-LEVEL (INVOICE DETAIL) OVERRIDE TESTS ===

    [Fact]
    public void MapToInvoicesDoc_WithLineItemCodeAndCommentsOverride_ShouldSetFields()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        itemCode = "PROD-001",
                        lineComments = "Special handling required"
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].itemCode.Should().Be("PROD-001");
        doc.invoice[0].invoiceDetails[0].lineComments.Should().Be("Special handling required");
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvalidChargeItemCaseData_ShouldNotThrow()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        // Set some unrelated data in ftChargeItemCaseData
        request.cbChargeItems[0].ftChargeItemCaseData = new { someOtherField = "value" };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
    }

    [Fact]
    public void ApplyInvoiceDetailOverride_WithAllFields_ShouldSetAllFields()
    {
        var row = new global::InvoiceRowType
        {
            lineNumber = 1,
            netValue = 100,
            vatAmount = 24,
            vatCategory = 1
        };

        var detailOverride = new InvoiceRowTypeOverride
        {
            TaricNo = "12345678",
            ItemCode = "ITEM-001",
            FuelCode = 10,
            LineComments = "Test comment",
            Quantity15 = 99.5m,
            OtherMeasurementUnitQuantity = 2,
            OtherMeasurementUnitTitle = "barrels",
            NotVAT195 = true
        };

        AADEFactory.ApplyInvoiceDetailOverride(row, detailOverride);

        row.TaricNo.Should().Be("12345678");
        row.itemCode.Should().Be("ITEM-001");
        row.fuelCodeSpecified.Should().BeTrue();
        row.lineComments.Should().Be("Test comment");
        row.quantity15Specified.Should().BeTrue();
        row.quantity15.Should().Be(99.5m);
        row.otherMeasurementUnitQuantitySpecified.Should().BeTrue();
        row.otherMeasurementUnitTitle.Should().Be("barrels");
        row.notVAT195Specified.Should().BeTrue();
        row.notVAT195.Should().BeTrue();
        row.vatCategory.Should().Be(1);
        row.netValue.Should().Be(100);
        row.vatAmount.Should().Be(24);
    }

    // === PHASE 2: INVOICE TYPE OVERRIDE TESTS ===

    [Theory]
    [InlineData("1.1", InvoiceType.Item11)]
    [InlineData("1.2", InvoiceType.Item12)]
    [InlineData("1.3", InvoiceType.Item13)]
    [InlineData("1.4", InvoiceType.Item14)]
    [InlineData("1.5", InvoiceType.Item15)]
    [InlineData("1.6", InvoiceType.Item16)]
    [InlineData("2.1", InvoiceType.Item21)]
    [InlineData("2.2", InvoiceType.Item22)]
    [InlineData("2.3", InvoiceType.Item23)]
    [InlineData("2.4", InvoiceType.Item24)]
    [InlineData("3.1", InvoiceType.Item31)]
    [InlineData("3.2", InvoiceType.Item32)]
    [InlineData("5.1", InvoiceType.Item51)]
    [InlineData("5.2", InvoiceType.Item52)]
    [InlineData("6.1", InvoiceType.Item61)]
    [InlineData("6.2", InvoiceType.Item62)]
    [InlineData("7.1", InvoiceType.Item71)]
    [InlineData("8.1", InvoiceType.Item81)]
    [InlineData("8.2", InvoiceType.Item82)]
    [InlineData("8.4", InvoiceType.Item84)]
    [InlineData("8.5", InvoiceType.Item85)]
    [InlineData("8.6", InvoiceType.Item86)]
    [InlineData("9.3", InvoiceType.Item93)]
    [InlineData("11.1", InvoiceType.Item111)]
    [InlineData("11.2", InvoiceType.Item112)]
    [InlineData("11.3", InvoiceType.Item113)]
    [InlineData("11.4", InvoiceType.Item114)]
    [InlineData("11.5", InvoiceType.Item115)]
    public void MapToInvoicesDoc_WithInvoiceTypeOverride_ShouldSetInvoiceType(string overrideValue, InvoiceType expected)
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            invoiceType = overrideValue
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceHeader.invoiceType.Should().Be(expected);
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvalidInvoiceTypeOverride_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            invoiceType = "99.9"
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("99.9");
    }

    // === PHASE 2: INCOME CLASSIFICATION OVERRIDE TESTS ===

    [Fact]
    public void MapToInvoicesDoc_WithIncomeClassificationOverride_ShouldReplaceClassification()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationType = "E3_880_001", classificationCategory = "category1_4" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].incomeClassification.Should().HaveCount(1);
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_880_001);
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationTypeSpecified.Should().BeTrue();
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_4);
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvalidIncomeClassificationType_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationType = "INVALID_TYPE", classificationCategory = "category1_1" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("INVALID_TYPE");
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvalidIncomeClassificationCategory_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationType = "E3_561_001", classificationCategory = "INVALID_CAT" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("INVALID_CAT");
    }

    [Fact]
    public void MapToInvoicesDoc_WithMultipleIncomeClassifications_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationType = "E3_561_001", classificationCategory = "category1_1" },
                            new { classificationType = "E3_561_002", classificationCategory = "category1_2" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("exactly one element");
    }

    // === PHASE 2: EXPENSES CLASSIFICATION OVERRIDE TESTS ===

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationOverride_ShouldReplaceClassification()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].expensesClassification.Should().HaveCount(1);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_001);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].classificationTypeSpecified.Should().BeTrue();
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_1);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].classificationCategorySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvalidExpensesClassificationType_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "BOGUS", classificationCategory = "category2_1" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("BOGUS");
    }

    [Fact]
    public void MapToInvoicesDoc_WithMultipleExpensesClassifications_ShouldCreateMultipleEntries()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 50.0m },
                            new { classificationType = "E3_102_002", classificationCategory = "category2_2", amount = 50.0m }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].expensesClassification.Should().HaveCount(2);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_001);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_1);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].amount.Should().Be(50.0m);
        doc.invoice[0].invoiceDetails[0].expensesClassification[1].classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_002);
        doc.invoice[0].invoiceDetails[0].expensesClassification[1].classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_2);
        doc.invoice[0].invoiceDetails[0].expensesClassification[1].amount.Should().Be(50.0m);
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationAmountOverride_ShouldUseProvidedAmount()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 75.0m }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].expensesClassification.Should().HaveCount(1);
        doc.invoice[0].invoiceDetails[0].expensesClassification[0].amount.Should().Be(75.0m);
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationSummary_ShouldGroupByAllDimensions()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                Quantity = 1,
                Description = "Item A",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new
                    {
                        mydataoverride = new
                        {
                            invoiceDetails = new
                            {
                                expensesClassification = new[]
                                {
                                    new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 40.0m, vatCategory = 1 },
                                    new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 40.0m, vatCategory = 2 }
                                }
                            }
                        }
                    }
                }
            },
            new ChargeItem
            {
                Amount = 100,
                Quantity = 1,
                Description = "Item B",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new
                    {
                        mydataoverride = new
                        {
                            invoiceDetails = new
                            {
                                expensesClassification = new[]
                                {
                                    new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 60.0m, vatCategory = 1 }
                                }
                            }
                        }
                    }
                }
            }
        };
        request.cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 200, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000 }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();

        // Summary should have 2 entries: same type/category but different vatCategory (1 vs 2)
        var summary = doc!.invoice[0].invoiceSummary.expensesClassification;
        summary.Should().HaveCount(2);

        var vatCat1 = summary.Single(x => x.vatCategory == 1);
        vatCat1.amount.Should().Be(100.0m); // 40 + 60
        vatCat1.classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_001);
        vatCat1.classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_1);
        vatCat1.vatCategorySpecified.Should().BeTrue();

        var vatCat2 = summary.Single(x => x.vatCategory == 2);
        vatCat2.amount.Should().Be(40.0m);
        vatCat2.classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_001);
        vatCat2.classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_1);
        vatCat2.vatCategorySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationNoAmount_ShouldDefaultToNetValue()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var line = doc!.invoice[0].invoiceDetails[0];
        line.expensesClassification[0].amount.Should().Be(line.netValue);
    }

    [Fact]
    public void MapToInvoicesDoc_WithMultipleExpensesClassificationsNoAmount_ShouldDefaultEachToNetValue()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1" },
                            new { classificationType = "E3_102_002", classificationCategory = "category2_2" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var line = doc!.invoice[0].invoiceDetails[0];
        line.expensesClassification.Should().HaveCount(2);
        line.expensesClassification[0].amount.Should().Be(line.netValue);
        line.expensesClassification[1].amount.Should().Be(line.netValue);
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationVatAmount_ShouldMapVatAmount()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 80.0m, vatAmount = 19.2m }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var ec = doc!.invoice[0].invoiceDetails[0].expensesClassification[0];
        ec.vatAmount.Should().Be(19.2m);
        ec.vatAmountSpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationVatCategory_ShouldMapVatCategory()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1", vatCategory = 1 }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var ec = doc!.invoice[0].invoiceDetails[0].expensesClassification[0];
        ec.vatCategory.Should().Be(1);
        ec.vatCategorySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationVatExemptionCategory_ShouldMapVatExemptionCategory()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1", vatExemptionCategory = 3 }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var ec = doc!.invoice[0].invoiceDetails[0].expensesClassification[0];
        ec.vatExemptionCategory.Should().Be(3);
        ec.vatExemptionCategorySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationId_ShouldMapId()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1", id = 5 }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var ec = doc!.invoice[0].invoiceDetails[0].expensesClassification[0];
        ec.id.Should().Be(5);
        ec.idSpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationAllFields_ShouldMapAllFields()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new
                            {
                                classificationType = "E3_102_001",
                                classificationCategory = "category2_1",
                                amount = 80.0m,
                                vatAmount = 19.2m,
                                vatCategory = 1,
                                vatExemptionCategory = 3,
                                id = 2
                            }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var ec = doc!.invoice[0].invoiceDetails[0].expensesClassification[0];
        ec.classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_001);
        ec.classificationTypeSpecified.Should().BeTrue();
        ec.classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_1);
        ec.classificationCategorySpecified.Should().BeTrue();
        ec.amount.Should().Be(80.0m);
        ec.vatAmount.Should().Be(19.2m);
        ec.vatAmountSpecified.Should().BeTrue();
        ec.vatCategory.Should().Be(1);
        ec.vatCategorySpecified.Should().BeTrue();
        ec.vatExemptionCategory.Should().Be(3);
        ec.vatExemptionCategorySpecified.Should().BeTrue();
        ec.id.Should().Be(2);
        ec.idSpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationOverride_ShouldClearIncomeClassification()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "category2_1" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].incomeClassification.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvalidExpensesClassificationCategory_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        expensesClassification = new[]
                        {
                            new { classificationType = "E3_102_001", classificationCategory = "INVALID_CAT" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("INVALID_CAT");
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationSummary_ShouldSumAmountsForSameGroup()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item A",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 30.0m }
                    }}}}
                }
            },
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item B",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 50.0m }
                    }}}}
                }
            }
        };
        request.cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 200, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000 }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var summary = doc!.invoice[0].invoiceSummary.expensesClassification;
        summary.Should().HaveCount(1);
        summary[0].amount.Should().Be(80.0m);
        summary[0].classificationType.Should().Be(ExpensesClassificationTypeClassificationType.E3_102_001);
        summary[0].classificationCategory.Should().Be(ExpensesClassificationCategoryType.category2_1);
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationSummary_ShouldGroupByVatExemptionCategory()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item A",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 40.0m, vatExemptionCategory = 1 }
                    }}}}
                }
            },
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item B",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 60.0m, vatExemptionCategory = 2 }
                    }}}}
                }
            }
        };
        request.cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 200, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000 }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var summary = doc!.invoice[0].invoiceSummary.expensesClassification;
        summary.Should().HaveCount(2);

        var exemptCat1 = summary.Single(x => x.vatExemptionCategory == 1);
        exemptCat1.amount.Should().Be(40.0m);
        exemptCat1.vatExemptionCategorySpecified.Should().BeTrue();

        var exemptCat2 = summary.Single(x => x.vatExemptionCategory == 2);
        exemptCat2.amount.Should().Be(60.0m);
        exemptCat2.vatExemptionCategorySpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationSummary_ShouldSumVatAmounts()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item A",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 40.0m, vatAmount = 9.6m }
                    }}}}
                }
            },
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item B",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 60.0m, vatAmount = 14.4m }
                    }}}}
                }
            }
        };
        request.cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 200, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000 }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var summary = doc!.invoice[0].invoiceSummary.expensesClassification;
        summary.Should().HaveCount(1);
        summary[0].amount.Should().Be(100.0m);
        summary[0].vatAmount.Should().Be(24.0m);
        summary[0].vatAmountSpecified.Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_WithExpensesClassificationSummary_ShouldSeparateDifferentClassificationTypes()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item A",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_001", classificationCategory = "category2_1", amount = 40.0m }
                    }}}}
                }
            },
            new ChargeItem
            {
                Amount = 100, Quantity = 1, Description = "Item B",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24,
                ftChargeItemCaseData = new
                {
                    GR = new { mydataoverride = new { invoiceDetails = new { expensesClassification = new[]
                    {
                        new { classificationType = "E3_102_002", classificationCategory = "category2_1", amount = 60.0m }
                    }}}}
                }
            }
        };
        request.cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 200, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000 }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var summary = doc!.invoice[0].invoiceSummary.expensesClassification;
        summary.Should().HaveCount(2);
        summary.Single(x => x.classificationType == ExpensesClassificationTypeClassificationType.E3_102_001).amount.Should().Be(40.0m);
        summary.Single(x => x.classificationType == ExpensesClassificationTypeClassificationType.E3_102_002).amount.Should().Be(60.0m);
    }

    [Fact]
    public void MapToInvoicesDoc_WithIncomeClassificationCategoryOnly_ShouldWork()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationCategory = "category1_95" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceDetails[0].incomeClassification.Should().HaveCount(1);
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationTypeSpecified.Should().BeFalse();
    }

    // === ISSUE #68: THIRD-PARTY SALE (1.4) END-TO-END ===

    [Fact]
    public void MapToInvoicesDoc_ThirdPartySale_1_4_ShouldGenerateCorrectXml()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = "3rd-party-sale-step1",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbCustomer = new
            {
                CustomerVATId = "EL026883248",
                CustomerName = "Αγοραστής Α.Ε.",
                CustomerStreet = "Σταδίου 15",
                CustomerZip = "10562",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR"
            },
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 10,
                    Description = "Προϊόντα ΤΡΙΤΟΥ Α.Ε. - Goods of TRITOU S.A.",
                    Amount = 1240.00m,
                    VATRate = 24.0m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATAmount = 240,
                    ftChargeItemCaseData = new
                    {
                        GR = new
                        {
                            mydataoverride = new
                            {
                                invoiceDetails = new
                                {
                                    incomeClassification = new[]
                                    {
                                        new { classificationType = "E3_561_001", classificationCategory = "category1_7" }
                                    }
                                }
                            }
                        }
                    }
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Description = "Bank Transfer",
                    Amount = 1240.00m,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0004
                }
            ],
            ftReceiptCaseData = new
            {
                GR = new
                {
                    mydataoverride = new
                    {
                        invoice = new
                        {
                            invoiceHeader = new
                            {
                                invoiceType = "1.4"
                            }
                        }
                    }
                }
            }
        };

        var response = CreateBasicReceiptResponse(request);
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Verify no errors
        error.Should().BeNull();
        doc.Should().NotBeNull();

        var invoice = doc!.invoice[0];

        // Verify invoice type overridden to 1.4
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item14);

        // Verify income classification overridden to category1_7 + E3_561_001
        invoice.invoiceDetails[0].incomeClassification.Should().HaveCount(1);
        invoice.invoiceDetails[0].incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_7);
        invoice.invoiceDetails[0].incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
        invoice.invoiceDetails[0].incomeClassification[0].classificationTypeSpecified.Should().BeTrue();

        // Verify classification amount equals net value (1240 - 240 VAT = 1000)
        invoice.invoiceDetails[0].incomeClassification[0].amount.Should().Be(1000.00m);

        // Verify summary aggregates the overridden classification with correct amount
        invoice.invoiceSummary.incomeClassification.Should().ContainSingle(ic =>
            ic.classificationCategory == IncomeClassificationCategoryType.category1_7);
        invoice.invoiceSummary.incomeClassification[0].amount.Should().Be(1000.00m);

        // Verify net value and totals
        invoice.invoiceDetails[0].netValue.Should().Be(1000.00m);
        invoice.invoiceDetails[0].vatAmount.Should().Be(240m);
        invoice.invoiceSummary.totalNetValue.Should().Be(1000.00m);
        invoice.invoiceSummary.totalVatAmount.Should().Be(240m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(1240.00m);

        // Generate XML and verify it serializes correctly
        var xml = AADEFactory.GenerateInvoicePayload(doc);
        xml.Should().Contain("<invoiceType>1.4</invoiceType>");
        xml.Should().Contain(">E3_561_001</classificationType>");
        xml.Should().Contain(">category1_7</classificationCategory>");
        xml.Should().Contain(">1000.00</amount>");
        xml.Should().Contain("026883248");
    }

    [Fact]
    public void MapToInvoicesDoc_IncomeClassificationOverride_AmountShouldMatchNetValue()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        // Set explicit values: Amount=124, VATRate=24, VATAmount=24 → netValue = 124 - 24 = 100
        request.ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.1" } } } } };
        request.cbChargeItems[0].Amount = 124m;
        request.cbChargeItems[0].VATRate = 24m;
        request.cbChargeItems[0].VATAmount = 24m;
        request.cbPayItems[0].Amount = 124m;
        request.cbChargeItems[0].ftChargeItemCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoiceDetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationType = "E3_561_001", classificationCategory = "category1_4" }
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var row = doc!.invoice[0].invoiceDetails[0];

        // netValue = Amount - VATAmount = 124 - 24 = 100
        row.netValue.Should().Be(100m);
        row.vatAmount.Should().Be(24m);

        // Classification amount must equal the line's netValue
        row.incomeClassification.Should().HaveCount(1);
        row.incomeClassification[0].amount.Should().Be(100m);

        // Summary must aggregate correctly
        doc.invoice[0].invoiceSummary.totalNetValue.Should().Be(100m);
        doc.invoice[0].invoiceSummary.incomeClassification[0].amount.Should().Be(100m);
    }

    // === AGENCY BUSINESS (NotOwnSales) TESTS ===

    [Fact]
    public void MapToInvoicesDoc_AgencyBusiness_PosReceipt_ShouldMapToInvoiceType_11_5()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = "agency-pos",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "Handmade jewelry (on behalf of third party)",
                    Amount = 62.00m,
                    VATRate = 24.0m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NormalVatRate)
                        .WithTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales),
                }
            ],
            cbPayItems =
            [
                new PayItem { Amount = 62.00m, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0005 }
            ]
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item115, "POS receipt with NotOwnSales should be 11.5");
        doc.invoice[0].invoiceDetails[0].incomeClassification.Should().ContainSingle();
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_7);

        var xml = AADEFactory.GenerateInvoicePayload(doc);
        xml.Should().Contain("<invoiceType>11.5</invoiceType>");
        xml.Should().Contain(">category1_7</classificationCategory>");
    }

    [Fact]
    public void MapToInvoicesDoc_AgencyBusiness_B2BInvoice_ShouldMapToInvoiceType_1_4()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = "agency-b2b",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbCustomer = new
            {
                CustomerVATId = "EL026883248",
                CustomerName = "Αγοραστής Α.Ε.",
                CustomerCountry = "GR"
            },
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 5,
                    Description = "Electronics (on behalf of third party)",
                    Amount = 620.00m,
                    VATRate = 24.0m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NormalVatRate)
                        .WithTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales),
                    VATAmount = 120,
                }
            ],
            cbPayItems =
            [
                new PayItem { Description = "Bank Transfer", Amount = 620.00m, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0004 }
            ]
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item14, "B2B invoice with NotOwnSales should be 1.4");
        doc.invoice[0].invoiceDetails[0].incomeClassification.Should().ContainSingle();
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_7);
        doc.invoice[0].invoiceDetails[0].incomeClassification[0].amount.Should().Be(500.00m, "netValue = 620 - 120 = 500");

        var xml = AADEFactory.GenerateInvoicePayload(doc);
        xml.Should().Contain("<invoiceType>1.4</invoiceType>");
        xml.Should().Contain(">category1_7</classificationCategory>");
    }

    [Fact]
    public void MapToInvoicesDoc_AgencyBusiness_MixedWithNonAgency_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = "agency-mixed",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1, Description = "Agency item", Amount = 50.00m, VATRate = 24.0m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NormalVatRate)
                        .WithTypeOfService(ChargeItemCaseTypeOfService.NotOwnSales),
                },
                new ChargeItem
                {
                    Quantity = 1, Description = "Own item", Amount = 30.00m, VATRate = 24.0m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NormalVatRate),
                }
            ],
            cbPayItems =
            [
                new PayItem { Amount = 80.00m, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001 }
            ]
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull("mixing agency and non-agency items is not allowed");
        error!.Exception.Message.Should().Contain("NotOwnSales");
    }

    // === COUNTERPART NAME TESTS ===

    [Fact]
    public void MapToInvoicesDoc_DomesticCustomer_ShouldNotSetCounterpartName()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = "domestic-name",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbCustomer = new
            {
                CustomerVATId = "EL026883248",
                CustomerName = "Πελάτης Α.Ε.",
                CustomerStreet = "Σταδίου 15",
                CustomerZip = "10562",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR"
            },
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1, Description = "Item", Amount = 124m, VATRate = 24m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATAmount = 24
                }
            ],
            cbPayItems =
            [
                new PayItem { Amount = 124m, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001 }
            ]
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].counterpart.Should().NotBeNull();
        doc.invoice[0].counterpart.name.Should().BeNull("domestic GR customers should not have name set");
        doc.invoice[0].counterpart.vatNumber.Should().Be("026883248");
        doc.invoice[0].counterpart.country.Should().Be(CountryType.GR);
    }

    [Fact]
    public void MapToInvoicesDoc_ForeignCustomer_ShouldSetCounterpartName()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = "foreign-name",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbCustomer = new
            {
                CustomerVATId = "DE123456789",
                CustomerName = "German GmbH",
                CustomerStreet = "Berliner Str. 1",
                CustomerZip = "10115",
                CustomerCity = "Berlin",
                CustomerCountry = "DE"
            },
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1, Description = "Item", Amount = 124m, VATRate = 24m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                    VATAmount = 24
                }
            ],
            cbPayItems =
            [
                new PayItem { Amount = 124m, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001 }
            ]
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc!.invoice[0].counterpart.Should().NotBeNull();
        doc.invoice[0].counterpart.name.Should().Be("German GmbH", "foreign customers must have name set");
        doc.invoice[0].counterpart.country.Should().Be(CountryType.DE);
    }

    // === COUNTERPART ADDRESS FIELD TESTS ===

    [Fact]
    public void MapToInvoicesDoc_CustomerWithPostalCodeAndCity_ShouldSetAddress()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002);
        request.cbCustomer = new
        {
            CustomerVATId = "EL026883248",
            CustomerZip = "10562",
            CustomerCity = "Αθηνών",
            CustomerCountry = "GR"
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var cp = doc!.invoice[0].counterpart;
        cp.address.Should().NotBeNull();
        cp.address.postalCode.Should().Be("10562");
        cp.address.city.Should().Be("Αθηνών");
        cp.address.street.Should().BeNull("street not provided");
        cp.address.number.Should().BeNull("house number not provided");
    }

    [Fact]
    public void MapToInvoicesDoc_CustomerWithStreetAndHouseNumber_ShouldSetBoth()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002);
        request.cbCustomer = new
        {
            CustomerVATId = "DE123456789",
            CustomerName = "Test GmbH",
            CustomerStreet = "Berliner Str.",
            CustomerHouseNumber = "42",
            CustomerZip = "10115",
            CustomerCity = "Berlin",
            CustomerCountry = "DE"
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var cp = doc!.invoice[0].counterpart;
        cp.address.Should().NotBeNull();
        cp.address.street.Should().Be("Berliner Str.");
        cp.address.number.Should().Be("42");
        cp.address.postalCode.Should().Be("10115");
        cp.address.city.Should().Be("Berlin");
    }

    [Fact]
    public void MapToInvoicesDoc_CustomerWithoutPostalCodeOrCity_ShouldNotSetAddress()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002);
        request.cbCustomer = new
        {
            CustomerVATId = "EL026883248",
            CustomerCountry = "GR"
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var cp = doc!.invoice[0].counterpart;
        cp.address.Should().BeNull("no postalCode/city provided");

        var xml = AADEFactory.GenerateInvoicePayload(doc!);
        xml.Should().NotContain("<address>", "no address element should be serialized");
    }

    [Fact]
    public void MapToInvoicesDoc_CustomerAddress_XmlShouldOmitEmptyStreetAndNumber()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002);
        request.cbCustomer = new
        {
            CustomerVATId = "EL026883248",
            CustomerZip = "10562",
            CustomerCity = "Αθηνών",
            CustomerCountry = "GR"
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var xml = AADEFactory.GenerateInvoicePayload(doc!);
        xml.Should().Contain("<postalCode>10562</postalCode>");
        xml.Should().Contain("<city>Αθηνών</city>");
        xml.Should().NotContain("<street");
        xml.Should().NotContain("<number");
    }

    [Fact]
    public void MapToInvoicesDoc_CustomerAddressWithAllFields_XmlShouldIncludeStreetAndNumber()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002);
        request.cbCustomer = new
        {
            CustomerVATId = "DE123456789",
            CustomerName = "Test GmbH",
            CustomerStreet = "Hauptstr.",
            CustomerHouseNumber = "7",
            CustomerZip = "80331",
            CustomerCity = "München",
            CustomerCountry = "DE"
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var xml = AADEFactory.GenerateInvoicePayload(doc!);
        xml.Should().Contain("<street>Hauptstr.</street>");
        xml.Should().Contain("<number>7</number>");
        xml.Should().Contain("<postalCode>80331</postalCode>");
        xml.Should().Contain("<city>München</city>");
    }

    [Fact]
    public void HasInvoiceTypeOverride_WithoutCaseData_ReturnsFalse()
    {
        var request = CreateBasicReceiptRequest();

        AADEFactory.HasInvoiceTypeOverride(request).Should().BeFalse();
    }

    [Fact]
    public void HasInvoiceTypeOverride_WithOverrideButNoInvoiceType_ReturnsFalse()
    {
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            dispatchDate = "2025-06-18"
                        }
                    }
                }
            }
        };

        AADEFactory.HasInvoiceTypeOverride(request).Should().BeFalse();
    }

    [Fact]
    public void HasInvoiceTypeOverride_WithInvoiceType_ReturnsTrue()
    {
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            invoiceType = "11.1"
                        }
                    }
                }
            }
        };

        AADEFactory.HasInvoiceTypeOverride(request).Should().BeTrue();
    }

    [Fact]
    public void MapToInvoicesDoc_ECommerceWithInvoiceTypeOverride_ShouldUseOverrideType()
    {
        var factory = CreateFactory();
        var request = CreateBasicReceiptRequest();
        request.ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.ECommerce0x0004);
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            invoiceType = "11.1"
                        }
                    }
                }
            }
        };
        var response = CreateBasicReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111);
    }
}
