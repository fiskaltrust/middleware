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
        });
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
            (queueId, queueItemId) => $"https://receipts.example.com/{queueId}/{queueItemId}");

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

    [Fact]
    public void MapToInvoicesDoc_WithoutReceiptBaseAddress_ShouldNotSetDownloadingInvoiceUrl()
    {
        // Arrange
        var factory = CreateFactory(); // No receiptBaseAddress
        var request = CreateBasicReceiptRequest();
        var response = CreateBasicReceiptResponse(request);
        response.ftQueueID = Guid.Parse("12345678-1234-1234-1234-123456789012");
        response.ftQueueItemID = Guid.Parse("87654321-4321-4321-4321-210987654321");

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].downloadingInvoiceUrl.Should().BeNull();
    }
}
