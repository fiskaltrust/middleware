using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class SendPaymentsMethodTests
{
    /// <summary>
    /// The pay item flag that triggers SendPaymentsMethod routing.
    /// Bit 32 (0x0000_0001_0000_0000) on the ftPayItemCase signals a local pay-item.
    /// </summary>
    private const long LocalPayItemFlag = 0x0000_0001_0000_0000;

    private static readonly ReceiptCase Pay0x3005Case =
        ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Pay0x3005);

    private static PayItemCase WithLocalFlag(PayItemCase payItemCase) =>
        (PayItemCase) ((long) payItemCase | LocalPayItemFlag);

    private static AADEFactory CreateFactory() =>
        new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData { VatId = "EL123456789" }
        }, "https://test.receipts.example.com");

    #region Flag and Case Detection

    [Fact]
    public void Pay0x3005_ShouldNotBeMistaken_ForOtherCases()
    {
        Pay0x3005Case.IsCase(ReceiptCase.PaymentTransfer0x0002).Should().BeFalse();
        Pay0x3005Case.IsCase(ReceiptCase.Order0x3004).Should().BeFalse();
        Pay0x3005Case.IsFlag(ReceiptCaseFlagsGR.HasTransportInformation).Should().BeFalse();
    }

    #endregion

    #region MapToPaymentMethodsDoc

    [Fact]
    public void MapToPaymentMethodsDoc_WithValidCardPayment_ShouldMapCorrectly()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem
            {
                Position = 1,
                Amount = 10.00m,
                Description = "POS Payment",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment))
            }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods.Should().HaveCount(1);
        doc.paymentMethods[0].invoiceMark.Should().Be(400001951868897);
        doc.paymentMethods[0].paymentMethodDetails.Should().HaveCount(1);
        doc.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
        doc.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(10.00m);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithEntityVatNumber_ShouldSetIt()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem
            {
                Position = 1,
                Amount = 50.00m,
                Description = "Cash",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment))
            }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897, "EL987654321");

        error.Should().BeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().Be("EL987654321");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithoutEntityVatNumber_ShouldLeaveItNull()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem
            {
                Position = 1,
                Amount = 10.00m,
                Description = "Cash",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment))
            }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 123456789);

        error.Should().BeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().BeNull();
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithNoPayItems_ShouldReturnError()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>());

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Exception.Message.Should().Be("At least one payment method detail is required for SendPaymentsMethod.");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithMultiplePayItems_ShouldMapAll()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 5.00m, Description = "Cash",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)) },
            new PayItem { Position = 2, Amount = 5.00m, Description = "Card",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        doc!.paymentMethods[0].paymentMethodDetails.Should().HaveCount(2);
        doc.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.Cash);
        doc.paymentMethods[0].paymentMethodDetails[1].type.Should().Be(MyDataPaymentMethods.PosEPos);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_ShouldRoundAmountsToTwoDecimals()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.555m, Description = "Cash",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 123456789);

        error.Should().BeNull();
        doc!.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(10.56m);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_PaymentMethodMarkShouldNotBeSet_AsItIsPopulatedByAADE()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.00m, Description = "Cash",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        doc!.paymentMethods[0].paymentMethodMarkSpecified.Should().BeFalse(
            "paymentMethodMark is populated by the AADE service in its response, not set by the caller");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithAadeSignatureData_ShouldExtractProviderSignature()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem
            {
                Position = 1,
                Amount = 10.00m,
                Description = "POS Payment",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)),
                ftPayItemCaseData = new
                {
                    aadeSignatureData = new
                    {
                        aadeProviderSignature = "test-provider-signature",
                        aadeTransactionId    = "test-transaction-id"
                    }
                }
            }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        var detail = doc!.paymentMethods[0].paymentMethodDetails[0];
        detail.ProvidersSignature.Should().NotBeNull(
            "AADE requires ProvidersSignature when the original invoice was submitted through a fiscal provider");
        detail.ProvidersSignature!.Signature.Should().Be("test-provider-signature");
        detail.ProvidersSignature.SigningAuthor.Should().Be("126");
        detail.transactionId.Should().Be("test-transaction-id");
    }

    #endregion

    #region GeneratePaymentMethodPayload

    [Fact]
    public void GeneratePaymentMethodPayload_ShouldProduceValidXml()
    {
        var doc = BuildPaymentMethodsDoc(400001960899044, MyDataPaymentMethods.PosEPos, 10.00m);

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().NotBeNullOrEmpty();
        payload.Should().Contain("PaymentMethodsDoc");
        payload.Should().Contain("paymentMethods");
        payload.Should().Contain("400001960899044");
        payload.Should().Contain("paymentMethodDetails");
    }

    [Fact]
    public void GeneratePaymentMethodPayload_ShouldContainCorrectNamespace()
    {
        var doc = BuildPaymentMethodsDoc(123, MyDataPaymentMethods.Cash, 10m);

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().Contain("https://www.aade.gr/myDATA/paymentMethod/v1.0");
    }

    [Fact]
    public void GeneratePaymentMethodPayload_ShouldBeRoundtrippable()
    {
        var doc = BuildPaymentMethodsDoc(400001951868897, MyDataPaymentMethods.PosEPos, 10.00m);

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        var xmlSerializer = new XmlSerializer(typeof(PaymentMethodsDoc));
        using var stringReader = new StringReader(payload);
        var deserialized = (PaymentMethodsDoc) xmlSerializer.Deserialize(stringReader)!;

        deserialized.paymentMethods.Should().HaveCount(1);
        deserialized.paymentMethods[0].invoiceMark.Should().Be(400001951868897);
        deserialized.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
        deserialized.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(10.00m);
    }

    [Fact]
    public void GeneratePaymentMethodPayload_WithEntityVatNumber_ShouldIncludeItInXml()
    {
        var doc = new PaymentMethodsDoc
        {
            paymentMethods = new[]
            {
                new PaymentMethodType
                {
                    invoiceMark = 123,
                    entityVatNumber = "123456789",
                    paymentMethodDetails = new[]
                    {
                        new PaymentMethodDetailType { type = MyDataPaymentMethods.Cash, amount = 10m }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().Contain("entityVatNumber");
        payload.Should().Contain("123456789");
    }

    [Fact]
    public void GeneratePaymentMethodPayload_WithProviderSignature_ShouldIncludeItInXml()
    {
        var doc = new PaymentMethodsDoc
        {
            paymentMethods = new[]
            {
                new PaymentMethodType
                {
                    invoiceMark = 400001951868897,
                    paymentMethodDetails = new[]
                    {
                        new PaymentMethodDetailType
                        {
                            type = MyDataPaymentMethods.PosEPos,
                            amount = 10.00m,
                            transactionId = "test-transaction-id",
                            ProvidersSignature = new ProviderSignatureType
                            {
                                Signature    = "test-provider-signature",
                                SigningAuthor = "126"
                            }
                        }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().Contain("ProvidersSignature");
        payload.Should().Contain("test-provider-signature");
        payload.Should().Contain("126");
        payload.Should().Contain("test-transaction-id");
    }

    #endregion

    #region ProcessReceiptAsync Routing

    [Fact]
    public void RoutingCondition_Pay0x3005WithLocalPayItemFlag_ShouldTrigger()
    {
        var receiptCase = Pay0x3005Case;
        var payItems = new List<PayItem>
        {
            new PayItem
            {
                Position = 1, Amount = 10.00m, Description = "Card",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment))
            }
        };
        var receiptReferences = new List<(ReceiptRequest, ReceiptResponse)> { BuildPreviousReceiptPair("400001951868897") };

        var hasLocalPayItemFlag = payItems.Any(p => ((long) p.ftPayItemCase & LocalPayItemFlag) != 0);
        bool shouldRoute =
            receiptCase.IsCase(ReceiptCase.Pay0x3005) &&
            hasLocalPayItemFlag &&
            receiptReferences != null && receiptReferences.Count > 0;

        shouldRoute.Should().BeTrue();
    }

    [Fact]
    public void RoutingCondition_Pay0x3005WithoutLocalPayItemFlag_ShouldNotTrigger()
    {
        var receiptCase = Pay0x3005Case;
        var payItems = new List<PayItem>
        {
            new PayItem
            {
                Position = 1, Amount = 10.00m, Description = "Card",
                ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)
            }
        };

        var hasLocalPayItemFlag = payItems.Any(p => ((long) p.ftPayItemCase & LocalPayItemFlag) != 0);
        bool shouldRoute =
            receiptCase.IsCase(ReceiptCase.Pay0x3005) &&
            hasLocalPayItemFlag;

        shouldRoute.Should().BeFalse("local pay item flag is missing");
    }

    [Fact]
    public void RoutingCondition_PaymentTransfer_ShouldNotTrigger()
    {
        var receiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002);

        bool shouldRoute = receiptCase.IsCase(ReceiptCase.Pay0x3005);

        shouldRoute.Should().BeFalse("PaymentTransfer0x0002 is a different receipt case that maps to an 8.4 invoice");
    }

    [Fact]
    public void RoutingCondition_Pay0x3005WithFlag_ButNoReceiptReferences_ShouldNotRoute()
    {
        var receiptCase = Pay0x3005Case;
        var payItems = new List<PayItem>
        {
            new PayItem
            {
                Position = 1, Amount = 10.00m, Description = "Card",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment))
            }
        };
        List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null;

        var hasLocalPayItemFlag = payItems.Any(p => ((long) p.ftPayItemCase & LocalPayItemFlag) != 0);
        bool shouldRoute =
            receiptCase.IsCase(ReceiptCase.Pay0x3005) &&
            hasLocalPayItemFlag &&
            receiptReferences != null && receiptReferences.Count > 0;

        shouldRoute.Should().BeFalse("cbPreviousReceiptReference and receiptReferences are required");
    }

    [Fact]
    public void RoutingCondition_ValidInvoiceMarkExtractedFromPreviousReceipt()
    {
        var previousResponse = BuildPreviousReceiptPair("400001951868897");
        var invoiceMarkText = previousResponse.Item2.ftSignatures
            .FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;

        long.TryParse(invoiceMarkText, out var invoiceMark).Should().BeTrue();
        invoiceMark.Should().Be(400001951868897);
    }

    [Fact]
    public void RoutingCondition_MissingInvoiceMarkOnPreviousReceipt_ShouldNotParseMark()
    {
        var previousResponse = new ReceiptResponse
        {
            cbReceiptReference = "prev",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>()
        };

        var invoiceMarkText = previousResponse.ftSignatures
            .FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;

        invoiceMarkText.Should().BeNull();
        long.TryParse(invoiceMarkText, out _).Should().BeFalse();
    }

    #endregion

    #region EntityVatNumber Extraction (Provider Endpoint)

    [Theory]
    [InlineData("EL112545020", "112545020")]
    [InlineData("GR112545020", "112545020")]
    [InlineData("112545020", "112545020")]
    [InlineData("EL123456789", "123456789")]
    public void EntityVatNumber_ShouldStripPrefixAndKeepDigitsOnly(string configuredVatId, string expectedEntityVat)
    {
        var entityVatNumber = new string(configuredVatId.Where(char.IsDigit).ToArray());

        entityVatNumber.Should().Be(expectedEntityVat);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithELPrefixedVat_ShouldSetDigitsOnlyEntityVatNumber()
    {
        var factory = CreateFactory();
        var entityVatNumber = new string("EL123456789".Where(char.IsDigit).ToArray());

        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.00m, Description = "POS Payment",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001960899044, entityVatNumber);

        error.Should().BeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().Be("123456789",
            "provider endpoint requires digits-only VAT; EL/GR prefix must be stripped");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithDigitsOnlyVat_ShouldPassThrough()
    {
        var factory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData { VatId = "112545020" }
        }, "https://test.receipts.example.com");

        var entityVatNumber = new string("112545020".Where(char.IsDigit).ToArray());

        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.00m, Description = "POS Payment",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001960899044, entityVatNumber);

        error.Should().BeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().Be("112545020");
    }

    [Fact]
    public void GeneratePaymentMethodPayload_WithEntityVatNumber_ShouldAppearInXmlBeforePaymentMethodDetails()
    {
        var doc = new PaymentMethodsDoc
        {
            paymentMethods = new[]
            {
                new PaymentMethodType
                {
                    invoiceMark = 400001960899044,
                    entityVatNumber = "112545020",
                    paymentMethodDetails = new[]
                    {
                        new PaymentMethodDetailType { type = MyDataPaymentMethods.PosEPos, amount = 10.00m }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().Contain("entityVatNumber");
        payload.Should().Contain("112545020");
        var entityVatPos = payload.IndexOf("entityVatNumber");
        var detailsPos = payload.IndexOf("paymentMethodDetails");
        entityVatPos.Should().BeLessThan(detailsPos,
            "entityVatNumber should appear before paymentMethodDetails in the XML per schema order");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithoutEntityVatNumber_ShouldNotIncludeItInXml()
    {
        var factory = CreateFactory();
        var receiptRequest = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.00m, Description = "POS Payment",
                ftPayItemCase = WithLocalFlag(((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001960899044);

        error.Should().BeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().BeNull();

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);
        payload.Should().NotContain("entityVatNumber",
            "when the entity calls directly (not via provider), entityVatNumber should be omitted");
    }

    #endregion

    #region Helpers

    private static ReceiptRequest BuildRequest(List<PayItem> payItems) =>
        new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = payItems,
            ftReceiptCase = Pay0x3005Case
        };

    private static PaymentMethodsDoc BuildPaymentMethodsDoc(long invoiceMark, int type, decimal amount) =>
        new PaymentMethodsDoc
        {
            paymentMethods = new[]
            {
                new PaymentMethodType
                {
                    invoiceMark = invoiceMark,
                    paymentMethodDetails = new[]
                    {
                        new PaymentMethodDetailType { type = type, amount = amount }
                    }
                }
            }
        };

    private static (ReceiptRequest, ReceiptResponse) BuildPreviousReceiptPair(string invoiceMark) =>
        (new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = "prev",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        },
        new ReceiptResponse
        {
            cbReceiptReference = "prev",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem
                {
                    Caption = "invoiceMark",
                    Data = invoiceMark,
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType) 0x4752_2000_0000_0000
                }
            }
        });

    #endregion
}
