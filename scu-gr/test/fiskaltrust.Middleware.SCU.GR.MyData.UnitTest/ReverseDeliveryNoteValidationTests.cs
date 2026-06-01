using System;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class ReverseDeliveryNoteValidationTests
{
    private static readonly MasterDataConfiguration _masterData = new()
    {
        Account = new AccountMasterData { VatId = "112545020", AccountName = "Test Company" },
        Outlet = new OutletMasterData { LocationId = "1", Street = "Test Street", City = "Athens", Zip = "12345" }
    };

    private static readonly AADEFactory _factory = new(_masterData, "https://test.receipts.example.com");

    private static readonly MiddlewareCustomer _greekCustomer = new()
    {
        CustomerVATId = "026883248",
        CustomerName = "Πελάτης A.E.",
        CustomerStreet = "Κηφισίας 12",
        CustomerZip = "12345",
        CustomerCity = "Αθηνών",
        CustomerCountry = "GR"
    };

    private static ReceiptResponse CreateResponse() => new()
    {
        cbReceiptReference = Guid.NewGuid().ToString(),
        ftReceiptIdentification = "ft1#",
        ftCashBoxIdentification = "TEST-001",
        ftQueueID = Guid.NewGuid(),
        ftQueueItemID = Guid.NewGuid()
    };

    /// <summary>
    /// Builds the ftReceiptCase for a reverse delivery note (9.3).
    /// 0x0500 = HasTransportInformation (0x0400) + Refund (0x0100) — matches spec ftReceiptCase: 0x0000_2000_0500_0005
    /// </summary>
    private static ReceiptCase CreateReverseDeliveryReceiptCase() =>
        ((ReceiptCase) 0x4752_2000_0000_0000)
            .WithCase(ReceiptCase.DeliveryNote0x0005)
            .WithFlag(ReceiptCaseFlagsGR.HasTransportInformation)
            .WithFlag(ReceiptCaseFlags.Refund);

    /// <summary>
    /// Builds the ftReceiptCase for a normal (non-reverse) 9.3 delivery note.
    /// No ReceiptCaseFlags.Refund — reverseDeliveryNote must NOT be set.
    /// </summary>
    private static ReceiptCase CreateNormalDeliveryReceiptCase() =>
        ((ReceiptCase) 0x4752_2000_0000_0000)
            .WithCase(ReceiptCase.DeliveryNote0x0005)
            .WithFlag(ReceiptCaseFlagsGR.HasTransportInformation);

    /// <summary>
    /// ftReceiptCaseData must be assigned as an object (NOT JsonSerializer.Serialize'd string)
    /// because TryDeserializeftReceiptCaseData calls JsonSerializer.Serialize(ftReceiptCaseData)
    /// internally — if ftReceiptCaseData is already a string it gets double-encoded.
    /// </summary>
    private static object BuildCaseData(int reverseDeliveryNotePurpose) => new
    {
        GR = new
        {
            mydataoverride = new
            {
                invoice = new
                {
                    invoiceHeader = new
                    {
                        dispatchDate = "2026-02-05T10:47:18Z",
                        dispatchTime = "2026-02-05T10:47:18Z",
                        movePurpose = 1,
                        reverseDeliveryNotePurpose,
                        otherDeliveryNoteHeader = new
                        {
                            loadingAddress = new { street = "Παπαδιαμάντη 24", number = "0", postalCode = "56429", city = "Νέα Ευκαρπία" },
                            deliveryAddress = new { street = "ΝΕΟΧΩΡΟΥΔΑ", number = "0", postalCode = "54500", city = "ΑΝΘΟΚΗΠΟΙ" },
                            startShippingBranch = 0,
                            completeShippingBranch = 0
                        }
                    }
                }
            }
        }
    };

    /// <summary>
    /// fiskaltrust caller sends Quantity negative (e.g. -1).
    /// Factory applies -x.Quantity via ReceiptCaseFlags.Refund → myDATA receives positive value.
    /// </summary>
    private static ReceiptRequest CreateReverseDeliveryNoteReceiptCase(int? reverseDeliveryNotePurpose = null)
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCaseData = reverseDeliveryNotePurpose.HasValue
                ? BuildCaseData(reverseDeliveryNotePurpose.Value)
                : null,
            ftReceiptCase = CreateReverseDeliveryReceiptCase(),
            cbCustomer = _greekCustomer,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Description = "Tablet return",
                    Amount = 0,
                    Quantity = -1,  // fiskaltrust caller sends negative — factory negates → myDATA receives +1
                    VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NotTaxable)
                        .WithFlag(ChargeItemCaseFlags.Refund)
                }
            ],
            cbPayItems = []
        };
    }

    [Fact]
    public void ReverseDeliveryNote_WithRefundFlag_ShouldSetReverseDeliveryNoteTrue()
    {
        // ReceiptCaseFlags.Refund on ftReceiptCase (0x0500) drives reverseDeliveryNote = true
        var request = CreateReverseDeliveryNoteReceiptCase();
        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());

        using var _ = new AssertionScope();
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var header = invoiceDoc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item93);
        header.reverseDeliveryNote.Should().BeTrue("ReceiptCaseFlags.Refund on ftReceiptCase must set reverseDeliveryNote = true");
        header.reverseDeliveryNoteSpecified.Should().BeTrue();
        header.currencySpecified.Should().BeFalse("9.3 does not allow currency");
    }

    [Fact]
    public void ReverseDeliveryNote_WithoutRefundFlag_ShouldNotSetReverseDeliveryNote()
    {
        // Normal 9.3 — NO ReceiptCaseFlags.Refund on ftReceiptCase
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = CreateNormalDeliveryReceiptCase(),
            cbCustomer = _greekCustomer,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1, Description = "Tablet", Amount = 0, Quantity = 1, VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            ],
            cbPayItems = []
        };

        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());

        error.Should().BeNull();
        invoiceDoc!.invoice[0].invoiceHeader.reverseDeliveryNoteSpecified.Should().BeFalse(
            "reverseDeliveryNote must not be set when ReceiptCaseFlags.Refund is absent from ftReceiptCase");
    }

    [Fact]
    public void ReverseDeliveryNote_WithPurposeOverride_ShouldSetReverseDeliveryNotePurpose()
    {
        var request = CreateReverseDeliveryNoteReceiptCase(reverseDeliveryNotePurpose: 5);
        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());

        using var _ = new AssertionScope();
        error.Should().BeNull();
        invoiceDoc.Should().NotBeNull();
        var header = invoiceDoc!.invoice[0].invoiceHeader;
        header.reverseDeliveryNote.Should().BeTrue();
        header.reverseDeliveryNoteSpecified.Should().BeTrue();
        header.reverseDeliveryNotePurpose.Should().Be(5);
        header.reverseDeliveryNotePurposeSpecified.Should().BeTrue();
    }

    [Fact]
    public void ReverseDeliveryNote_ChargeItemQuantities_ShouldBePositiveInXmlPayload()
    {
        // fiskaltrust caller sends Quantity = -1 (negative)
        // factory applies -x.Quantity via ReceiptCaseFlags.Refund → myDATA receives +1 (positive)
        var request = CreateReverseDeliveryNoteReceiptCase(reverseDeliveryNotePurpose: 5);
        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());

        error.Should().BeNull();
        invoiceDoc!.invoice[0].invoiceDetails[0].quantity.Should().BeGreaterThan(0,
            "factory negates quantity via ReceiptCaseFlags.Refund — myDATA requires positive values");
    }

    [Fact]
    public void ReverseDeliveryNote_PurposeNotApplied_WhenReverseDeliveryNoteNotSet()
    {
        // Purpose override is ignored when reverseDeliveryNote is not set
        // (i.e. ReceiptCaseFlags.Refund is absent from ftReceiptCase)
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCaseData = new
            {
                GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { dispatchDate = "2026-02-05T10:47:18Z", dispatchTime = "2026-02-05T10:47:18Z", movePurpose = 1, reverseDeliveryNotePurpose = 3 } } } }
            },
            ftReceiptCase = CreateNormalDeliveryReceiptCase(), // NO Refund flag
            cbCustomer = _greekCustomer,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1, Description = "Tablet", Amount = 0, Quantity = 1, VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            ],
            cbPayItems = []
        };

        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());

        using var _ = new AssertionScope();
        error.Should().BeNull();
        var header = invoiceDoc!.invoice[0].invoiceHeader;
        header.reverseDeliveryNoteSpecified.Should().BeFalse("no Refund flag on ftReceiptCase → reverseDeliveryNote not set");
        header.reverseDeliveryNotePurposeSpecified.Should().BeFalse("purpose is not applied when reverseDeliveryNote is not set");
    }

    [Fact]
    public void ReverseDeliveryNote_GeneratedXml_ShouldContainCorrectTags()
    {
        var request = CreateReverseDeliveryNoteReceiptCase(reverseDeliveryNotePurpose: 5);
        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());
        var xml = AADEFactory.GenerateInvoicePayload(invoiceDoc!);

        using var _ = new AssertionScope();
        error.Should().BeNull();
        xml.Should().Contain("<reverseDeliveryNote>true</reverseDeliveryNote>");
        xml.Should().Contain("<reverseDeliveryNotePurpose>5</reverseDeliveryNotePurpose>");
        xml.Should().Contain("<invoiceType>9.3</invoiceType>");
        xml.Should().NotContain("<currency>", "9.3 must not have currency");
    }

    [Fact]
    public void ReverseDeliveryNote_WithRefundFlag_AndMissingPurpose_ShouldReturnError()
    {
        // AADE rule: reverseDeliveryNotePurpose is mandatory when reverseDeliveryNote = true
        // Override present but reverseDeliveryNotePurpose intentionally omitted
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = CreateReverseDeliveryReceiptCase(),
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
                                dispatchDate = "2026-02-05T10:47:18Z",
                                dispatchTime = "2026-02-05T10:47:18Z",
                                movePurpose = 1
                                // reverseDeliveryNotePurpose intentionally omitted
                            }
                        }
                    }
                }
            },
            cbCustomer = _greekCustomer,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1, Description = "Tablet return", Amount = 0, Quantity = -1, VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NotTaxable)
                        .WithFlag(ChargeItemCaseFlags.Refund)
                }
            ],
            cbPayItems = []
        };

        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());

        error.Should().NotBeNull("reverseDeliveryNotePurpose is mandatory when reverseDeliveryNote = true");
        invoiceDoc.Should().BeNull();
    }

    [Fact]
    public void ReverseDeliveryNote_SpecExample_GenerateXmlForManualMyDataValidation()
    {
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = "1234",
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = CreateReverseDeliveryReceiptCase(), // 0x0000_2000_0500_0005
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "026883248",
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12",
                CustomerZip = "12345",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR"
            },
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
                                dispatchDate = "2026-11-27T15:22:07Z",
                                dispatchTime = "2026-11-27T15:22:07Z",
                                movePurpose = 1,
                                vehicleNumber = "ΝΒΧ8311",
                                reverseDeliveryNotePurpose = 5,
                                otherDeliveryNoteHeader = new
                                {
                                    loadingAddress = new { street = "Παπαδιαμάντη 24", number = "0", postalCode = "56429", city = "Νέα Ευκαρπία - Θεσσαλονίκη" },
                                    deliveryAddress = new { street = "ΙΚΤΙΝΟΥ 22", number = "0", postalCode = "54622", city = "ΘΕΣΣΑΛΟΝΙΚΗ" },
                                    startShippingBranch = 0,
                                    completeShippingBranch = 0
                                }
                            }
                        }
                    }
                }
            },
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Description = "Tablet",
                    Amount = 0,
                    Quantity = -1,  // fiskaltrust caller sends negative — factory negates → myDATA receives +1
                    VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NotTaxable)
                        .WithFlag(ChargeItemCaseFlags.Refund)
                },
                new ChargeItem
                {
                    Position = 2,
                    Description = "Laptop",
                    Amount = 0,
                    Quantity = -7,  // fiskaltrust caller sends negative — factory negates → myDATA receives +7
                    VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithVat(ChargeItemCase.NotTaxable)
                        .WithFlag(ChargeItemCaseFlags.Refund)
                }
            ],
            cbPayItems = []
        };

        var (invoiceDoc, error) = _factory.MapToInvoicesDoc(request, CreateResponse());
        var xml = AADEFactory.GenerateInvoicePayload(invoiceDoc!);

        // Validated successfully against myDATA sandbox — XML was accepted by AADE API
        using var _ = new AssertionScope();
        error.Should().BeNull("spec example must map without errors");
        invoiceDoc.Should().NotBeNull();

        var header = invoiceDoc!.invoice[0].invoiceHeader;

        // Invoice type must be 9.3
        header.invoiceType.Should().Be(InvoiceType.Item93);

        // reverseDeliveryNote must be true — driven by ReceiptCaseFlags.Refund on ftReceiptCase
        header.reverseDeliveryNote.Should().BeTrue();
        header.reverseDeliveryNoteSpecified.Should().BeTrue();

        // reverseDeliveryNotePurpose = 5 (REVERSAL OF OBLIGATION) — from override
        header.reverseDeliveryNotePurpose.Should().Be(5);
        header.reverseDeliveryNotePurposeSpecified.Should().BeTrue();

        // 9.3 must NOT have currency
        header.currencySpecified.Should().BeFalse();

        // vehicleNumber from override
        header.vehicleNumber.Should().Be("ΝΒΧ8311");

        // movePurpose from override
        header.movePurpose.Should().Be(1);
        header.movePurposeSpecified.Should().BeTrue();

        // loading address from override
        header.otherDeliveryNoteHeader.Should().NotBeNull();
        header.otherDeliveryNoteHeader.loadingAddress.street.Should().Be("Παπαδιαμάντη 24");
        header.otherDeliveryNoteHeader.loadingAddress.city.Should().Be("Νέα Ευκαρπία - Θεσσαλονίκη");

        // delivery address from override
        header.otherDeliveryNoteHeader.deliveryAddress.street.Should().Be("ΙΚΤΙΝΟΥ 22");
        header.otherDeliveryNoteHeader.deliveryAddress.city.Should().Be("ΘΕΣΣΑΛΟΝΙΚΗ");

        // 2 charge items — factory negates Quantity via ReceiptCaseFlags.Refund → myDATA receives positive values
        invoiceDoc.invoice[0].invoiceDetails.Should().HaveCount(2);
        invoiceDoc.invoice[0].invoiceDetails[0].quantity.Should().BeGreaterThan(0,
            "factory negates -1 → myDATA receives +1");
        invoiceDoc.invoice[0].invoiceDetails[1].quantity.Should().BeGreaterThan(0,
            "factory negates -7 → myDATA receives +7");

        // XML output — key tags verified (payload accepted by myDATA sandbox)
        xml.Should().Contain("<invoiceType>9.3</invoiceType>");
        xml.Should().Contain("<reverseDeliveryNote>true</reverseDeliveryNote>");
        xml.Should().Contain("<reverseDeliveryNotePurpose>5</reverseDeliveryNotePurpose>");
        xml.Should().NotContain("<currency>");
    }
}