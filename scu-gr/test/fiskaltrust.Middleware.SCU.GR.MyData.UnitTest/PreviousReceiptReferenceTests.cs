using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class PreviousReceiptReferenceTests
{
    private static AADEFactory CreateFactory() => new(new MasterDataConfiguration
    {
        Account = new AccountMasterData { VatId = "112545020" },
        Outlet = new OutletMasterData { LocationId = "0" }
    }, "https://receipts.example.com");

    private static ReceiptRequest CreatePosReceiptRequest()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 5, 5, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "T-Shirt Red XL",
                    Amount = -35.80m,
                    VATRate = 24m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate)
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Description = "Cash",
                    Amount = -35.80m,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            }
        };
    }

    private static ReceiptResponse CreatePosReceiptResponse(ReceiptRequest request) => new()
    {
        cbReceiptReference = request.cbReceiptReference,
        ftCashBoxIdentification = "TEST-001",
        ftReceiptIdentification = "ft123#"
    };

    // B2B refunds (case 0x1002 + Refund) resolve to Item 5.1, which supports correlatedInvoices
    // rather than multipleConnectedMarks — exercising the other branch in CollectPreviousMarks'
    // consumer logic.
    private static ReceiptRequest CreateB2BRefundRequest()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 5, 5, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.InvoiceB2B0x1002)
                .WithFlag(ReceiptCaseFlags.Refund),
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "123456789",
                CustomerCountry = "GR"
            },
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "Consulting service",
                    Amount = -100m,
                    VATRate = 24m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate)
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Description = "Cash",
                    Amount = -100m,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            }
        };
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMark_PopulatesCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400123456789"
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMarkAsArray_PopulatesAllMarks()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = new[] { "400123456789", "400987654321" }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L, 400987654321L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMarkAsLong_PopulatesCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = 400123456789L
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_NonRefund_PosReceipt_WithExternalInvoiceMark_PopulatesMultipleConnectedMarks()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.cbChargeItems[0].Amount = 35.80m;
        request.cbPayItems[0].Amount = 35.80m;
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400123456789"
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        // Retail/POS (Item11 family) supports both correlatedInvoices and multipleConnectedMarks;
        // the factory prefers multipleConnectedMarks for non-refund flows when available.
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMarkAndReceiptReferences_MergesBoth()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.cbPreviousReceiptReference = "internal-ref";
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400999999999"
                }
            }
        };

        var refRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = "internal-ref",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        };
        var refResponse = new ReceiptResponse
        {
            cbReceiptReference = "internal-ref",
            ftCashBoxIdentification = "TEST-001",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem { Caption = "invoiceMark", Data = "400111111111" }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(
            request,
            response,
            new List<(ReceiptRequest, ReceiptResponse)> { (refRequest, refResponse) });

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().Equal(400111111111L, 400999999999L);
    }

    [Fact]
    public void MapToInvoicesDoc_RestaurantOrderVoid_WithOnlyExternalInvoiceMark_Succeeds()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.Order0x3004)
                .WithFlag(ReceiptCaseFlags.Void),
            cbTerminalID = "1",
            cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbArea = "105",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "VOID item",
                    Amount = 0.0m,
                    VATRate = 0,
                    VATAmount = 0,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithFlag(ChargeItemCaseFlags.Void)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            },
            cbPayItems = new List<PayItem>(),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    PreviousReceiptReference = new
                    {
                        invoiceMark = "400123456789"
                    }
                }
            }
        };

        var response = new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "CB001",
            ftReceiptIdentification = "ft123#"
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item86);
        header.totalCancelDeliveryOrders.Should().BeTrue();
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_RestaurantOrderVoid_NoMarksAtAll_ReturnsError()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.Order0x3004)
                .WithFlag(ReceiptCaseFlags.Void),
            cbTerminalID = "1",
            cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbArea = "105",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "VOID item",
                    Amount = 0.0m,
                    VATRate = 0,
                    VATAmount = 0,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithFlag(ChargeItemCaseFlags.Void)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            },
            cbPayItems = new List<PayItem>()
        };

        var response = new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "CB001",
            ftReceiptIdentification = "ft123#"
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        // Message must enumerate all accepted correlation sources so the caller can pick one.
        error!.Exception.Message.Should().Contain("cbPreviousReceiptReference");
        error.Exception.Message.Should().Contain("PreviousReceiptReference.invoiceMark");
        error.Exception.Message.Should().Contain("mydataoverride");
        doc.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_RestaurantOrderVoid_WithOnlyMyDataOverrideCorrelatedInvoices_Succeeds()
    {
        // 8.6 VOID may also be satisfied by override-supplied correlations: marks placed onto
        // invoiceHeader.correlatedInvoices via mydataoverride count as a previous reference,
        // matching AADEMappings.HasAnyPreviousInvoiceReference. The override values survive
        // because CollectPreviousMarks finds nothing of its own and so does not overwrite them.
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.Order0x3004)
                .WithFlag(ReceiptCaseFlags.Void),
            cbTerminalID = "1",
            cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbArea = "105",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "VOID item",
                    Amount = 0.0m,
                    VATRate = 0,
                    VATAmount = 0,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithFlag(ChargeItemCaseFlags.Void)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            },
            cbPayItems = new List<PayItem>(),
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
                                correlatedInvoices = new[] { 400123456789L }
                            }
                        }
                    }
                }
            }
        };

        var response = new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "CB001",
            ftReceiptIdentification = "ft123#"
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item86);
        header.totalCancelDeliveryOrders.Should().BeTrue();
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400123456789L });
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_RestaurantOrderVoid_WithOnlyMyDataOverrideMultipleConnectedMarks_Succeeds()
    {
        // Mirror of the correlatedInvoices override-only case for the other field. Either
        // override-supplied field counts as a previous reference for the 8.6 VOID guard.
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.Order0x3004)
                .WithFlag(ReceiptCaseFlags.Void),
            cbTerminalID = "1",
            cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbArea = "105",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "VOID item",
                    Amount = 0.0m,
                    VATRate = 0,
                    VATAmount = 0,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithFlag(ChargeItemCaseFlags.Void)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            },
            cbPayItems = new List<PayItem>(),
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
                                multipleConnectedMarks = new[] { 400123456789L }
                            }
                        }
                    }
                }
            }
        };

        var response = new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "CB001",
            ftReceiptIdentification = "ft123#"
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item86);
        header.totalCancelDeliveryOrders.Should().BeTrue();
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
        header.correlatedInvoices.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithMyDataOverrideCorrelatedInvoices_PopulatesHeader()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
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
                            correlatedInvoices = new[] { 400123456789L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithMyDataOverrideMultipleConnectedMarks_PopulatesHeader()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
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
                            multipleConnectedMarks = new[] { 400123456789L, 400987654321L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L, 400987654321L });
    }

    [Fact]
    public void MapToInvoicesDoc_PreviousReceiptReferenceWinsOverMyDataOverride()
    {
        // When both ftReceiptCaseData.GR.PreviousReceiptReference.invoiceMark and
        // ftReceiptCaseData.GR.mydataoverride.invoice.invoiceHeader.correlatedInvoices/
        // multipleConnectedMarks are set, the dedicated PreviousReceiptReference path wins
        // because CollectPreviousMarks runs after ApplyMyDataOverride and overwrites the
        // header field. Callers should pick one of the two paths — this test pins down the
        // current precedence so future refactors don't silently swap it.
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400111111111"
                },
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            correlatedInvoices = new[] { 400222222222L },
                            multipleConnectedMarks = new[] { 400333333333L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400111111111L });
        // The override-supplied correlatedInvoices remains untouched on this invoice type
        // (Item114 only writes multipleConnectedMarks); both fields exist on the wire.
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400222222222L });
    }

    [Fact]
    public void MapToInvoicesDoc_B2BRefund_WithExternalInvoiceMark_PopulatesCorrelatedInvoices()
    {
        // Counterpart to the retail/POS refund tests: a B2B credit note (Item 5.1) routes the
        // collected marks into invoiceHeader.correlatedInvoices instead of multipleConnectedMarks.
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400123456789"
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51);
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400123456789L });
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_B2BRefund_WithExternalInvoiceMarkAsArray_PopulatesCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = new[] { "400123456789", "400987654321" }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51);
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400123456789L, 400987654321L });
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_B2BRefund_WithExternalInvoiceMarkAsLong_PopulatesCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = 400123456789L
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51);
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400123456789L });
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_B2BRefund_WithExternalInvoiceMarkAndReceiptReferences_MergesBothInCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.cbPreviousReceiptReference = "internal-b2b-ref";
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400999999999"
                }
            }
        };

        var refRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = "internal-b2b-ref",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002)
        };
        var refResponse = new ReceiptResponse
        {
            cbReceiptReference = "internal-b2b-ref",
            ftCashBoxIdentification = "TEST-001",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem { Caption = "invoiceMark", Data = "400111111111" }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(
            request,
            response,
            new List<(ReceiptRequest, ReceiptResponse)> { (refRequest, refResponse) });

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51);
        header.correlatedInvoices.Should().Equal(400111111111L, 400999999999L);
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_B2BRefund_WithMyDataOverrideCorrelatedInvoices_PopulatesHeader()
    {
        // Override-only flow on a B2B credit note: the override-supplied marks survive even
        // though CollectPreviousMarks finds nothing of its own, because ApplyMyDataOverride
        // wrote them onto the header earlier and the previousMarks.Length == 0 branch leaves
        // the field alone.
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
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
                            correlatedInvoices = new[] { 400123456789L, 400987654321L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51);
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400123456789L, 400987654321L });
    }

    [Fact]
    public void MapToInvoicesDoc_B2BRefund_PreviousReceiptReferenceWinsOverMyDataOverride()
    {
        // Mirror of the Item114 precedence test but on the correlatedInvoices branch: the
        // dedicated PreviousReceiptReference path overwrites whatever ApplyMyDataOverride
        // placed onto correlatedInvoices because CollectPreviousMarks runs afterwards.
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "400111111111"
                },
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            correlatedInvoices = new[] { 400222222222L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51);
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { 400111111111L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithNonNumericInvoiceMark_SilentlyDropsEntireCaseData()
    {
        // Pins current behavior: TryDeserializeftReceiptCaseData (Helpers/ReceiptRequestExtensions.cs)
        // catches every Exception, including the JsonException raised by
        // SingleOrListLongJsonConverter when the value is non-numeric. The entire
        // ftReceiptCaseData payload is therefore lost — including a co-supplied mydataoverride —
        // and the document emerges with NO correlations and the uncorrelated invoice type (5.2
        // instead of 5.1). If the deserialization error is ever surfaced or the converter is
        // made more permissive, this test must change.
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "not-a-number"
                },
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            correlatedInvoices = new[] { 400123456789L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item52);
        header.correlatedInvoices.Should().BeNull();
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void MapToInvoicesDoc_WithInvoiceMarkExceedingLongMax_SilentlyDropsEntireCaseData()
    {
        // Same silent-failure mode as the non-numeric case, but triggered by long.TryParse
        // returning false for a string of more than 19 digits. Documents the gotcha — see
        // SingleOrListLongJsonConverter.ReadSingleLong + TryDeserializeftReceiptCaseData.
        var factory = CreateFactory();
        var request = CreateB2BRefundRequest();
        request.ftReceiptCaseData = new
        {
            GR = new
            {
                PreviousReceiptReference = new
                {
                    invoiceMark = "99999999999999999999"
                },
                mydataoverride = new
                {
                    invoice = new
                    {
                        invoiceHeader = new
                        {
                            correlatedInvoices = new[] { 400123456789L }
                        }
                    }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item52);
        header.correlatedInvoices.Should().BeNull();
        header.multipleConnectedMarks.Should().BeNull();
    }
}
