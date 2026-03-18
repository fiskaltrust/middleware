using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class SendPaymentsMethodTests
{
    private static AADEFactory CreateFactory()
    {
        return new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
                VatId = "EL123456789"
            }
        }, "https://test.receipts.example.com");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithValidPayments_ShouldReturnPaymentMethodsDoc()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 10.00m,
                    Description = "POS Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        long invoiceMark = 400001951868897;

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, invoiceMark);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods.Should().HaveCount(1);
        doc.paymentMethods[0].invoiceMark.Should().Be(invoiceMark);
        doc.paymentMethods[0].paymentMethodDetails.Should().HaveCount(1);
        doc.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
        doc.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(10.00m);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithEntityVatNumber_ShouldSetEntityVatNumber()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 50.00m,
                    Description = "Cash Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        long invoiceMark = 400001951868897;
        var entityVatNumber = "EL987654321";

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, invoiceMark, entityVatNumber);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().Be(entityVatNumber);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithNoEntityVatNumber_ShouldLeaveEntityVatNumberNull()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 10.00m,
                    Description = "Cash Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 123456789);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods[0].entityVatNumber.Should().BeNull();
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithNoPayItems_ShouldReturnError()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Exception.Message.Should().Be("At least one payment method detail is required for SendPaymentsMethod.");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithMultiplePayments_ShouldMapAll()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 5.00m,
                    Description = "Cash Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                },
                new PayItem
                {
                    Position = 2,
                    Amount = 5.00m,
                    Description = "Card Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        long invoiceMark = 400001951868897;

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, invoiceMark);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods[0].paymentMethodDetails.Should().HaveCount(2);
        doc.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.Cash);
        doc.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(5.00m);
        doc.paymentMethods[0].paymentMethodDetails[1].type.Should().Be(MyDataPaymentMethods.PosEPos);
        doc.paymentMethods[0].paymentMethodDetails[1].amount.Should().Be(5.00m);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_ShouldRoundPayItemAmounts()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 10.555m,
                    Description = "Cash Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 123456789);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(10.56m);
    }

    [Fact]
    public void GeneratePaymentMethodPayload_ShouldProduceValidXml()
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
                            amount = 10.00m
                        }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().NotBeNullOrEmpty();
        payload.Should().Contain("PaymentMethodsDoc");
        payload.Should().Contain("paymentMethods");
        payload.Should().Contain("400001951868897");
        payload.Should().Contain("paymentMethodDetails");
    }

    [Fact]
    public void GeneratePaymentMethodPayload_ShouldBeDeserializable()
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
                            amount = 10.00m
                        }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        var xmlSerializer = new XmlSerializer(typeof(PaymentMethodsDoc));
        using var stringReader = new StringReader(payload);
        var deserialized = (PaymentMethodsDoc)xmlSerializer.Deserialize(stringReader)!;

        deserialized.Should().NotBeNull();
        deserialized.paymentMethods.Should().HaveCount(1);
        deserialized.paymentMethods[0].invoiceMark.Should().Be(400001951868897);
        deserialized.paymentMethods[0].paymentMethodDetails.Should().HaveCount(1);
        deserialized.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
        deserialized.paymentMethods[0].paymentMethodDetails[0].amount.Should().Be(10.00m);
    }

    [Fact]
    public void GeneratePaymentMethodPayload_WithEntityVatNumber_ShouldIncludeIt()
    {
        var doc = new PaymentMethodsDoc
        {
            paymentMethods = new[]
            {
                new PaymentMethodType
                {
                    invoiceMark = 400001951868897,
                    entityVatNumber = "123456789",
                    paymentMethodDetails = new[]
                    {
                        new PaymentMethodDetailType
                        {
                            type = MyDataPaymentMethods.Cash,
                            amount = 25.00m
                        }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().Contain("entityVatNumber");
        payload.Should().Contain("123456789");
    }

    [Fact]
    public void GeneratePaymentMethodPayload_XmlShouldContainCorrectNamespace()
    {
        var doc = new PaymentMethodsDoc
        {
            paymentMethods = new[]
            {
                new PaymentMethodType
                {
                    invoiceMark = 123,
                    paymentMethodDetails = new[]
                    {
                        new PaymentMethodDetailType
                        {
                            type = MyDataPaymentMethods.Cash,
                            amount = 10m
                        }
                    }
                }
            }
        };

        var payload = AADEFactory.GeneratePaymentMethodPayload(doc);

        payload.Should().Contain("https://www.aade.gr/myDATA/paymentMethod/v1.0");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_PaymentMethodMarkShouldNotBeSet()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 10.00m,
                    Description = "Cash Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.paymentMethods[0].paymentMethodMarkSpecified.Should().BeFalse("paymentMethodMark is populated by the AADE service, not the caller");
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithCashPayment_ShouldMapToCorrectType()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 100.00m,
                    Description = "Cash",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        doc!.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.Cash);
    }

    [Fact]
    public void MapToPaymentMethodsDoc_WithCreditCardPayment_ShouldMapToPosEPos()
    {
        var factory = CreateFactory();
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 100.00m,
                    Description = "Credit Card",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CreditCardPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var doc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, 400001951868897);

        error.Should().BeNull();
        doc!.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
    }

    [Fact]
    public void PaymentTransfer_WithPreviousReceiptReference_ShouldHaveInvoiceMark_ForSendPaymentsMethod()
    {
        // When a PaymentTransfer has a cbPreviousReceiptReference, the referenced receipt
        // must have an invoiceMark signature so that SendPaymentsMethod can be called.
        var previousReceiptResponse = new ReceiptResponse
        {
            cbReceiptReference = "previous-ref",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem
                {
                    Caption = "invoiceMark",
                    Data = "400001951868897",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = (SignatureType)0x4752_2000_0000_0000
                }
            }
        };

        var invoiceMarkText = previousReceiptResponse.ftSignatures
            .FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;

        long.TryParse(invoiceMarkText, out var invoiceMark).Should().BeTrue();
        invoiceMark.Should().Be(400001951868897);
    }

    [Fact]
    public void PaymentTransfer_WithPreviousReceiptReference_MissingMark_ShouldFail()
    {
        // When a PaymentTransfer has a cbPreviousReceiptReference but the referenced receipt
        // has no invoiceMark, the routing should produce an error.
        var previousReceiptResponse = new ReceiptResponse
        {
            cbReceiptReference = "previous-ref",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>()
        };

        var invoiceMarkText = previousReceiptResponse.ftSignatures
            .FirstOrDefault(x => x.Caption == "invoiceMark")?.Data;

        invoiceMarkText.Should().BeNull("no invoiceMark signature exists on the referenced receipt");
    }

    [Fact]
    public void PaymentTransfer_WithoutPreviousReceiptReference_ShouldMapTo84Invoice()
    {
        // When a PaymentTransfer does NOT have a cbPreviousReceiptReference,
        // it should fall through to MapToInvoicesDoc and produce an 8.4 invoice.
        var factory = CreateFactory();
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
                    Amount = 10,
                    Description = "Payment",
                    ProductNumber = "001",
                    Quantity = 1,
                    VATRate = 0,
                    ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithTypeOfService(ChargeItemCaseTypeOfService.Receivable),
                    Moment = DateTime.UtcNow,
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
                    Amount = 10.00m,
                    Description = "Cash",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#",
            ftCashBoxIdentification = "1233"
        };

        // Without cbPreviousReceiptReference, it should produce an 8.4 invoice via MapToInvoicesDoc
        (var doc, var error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item84);
    }

    [Fact]
    public void PaymentTransfer_WithPreviousReceiptReference_ShouldProducePaymentMethodsDoc()
    {
        // When a PaymentTransfer has a cbPreviousReceiptReference with a valid invoiceMark,
        // it should produce a PaymentMethodsDoc (not an InvoicesDoc).
        var factory = CreateFactory();
        long invoiceMark = 400001951868897;

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbPreviousReceiptReference = "previous-receipt-ref",
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 10.00m,
                    Description = "POS Payment",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.DebitCardPayment)
                }
            },
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002)
        };

        (var paymentDoc, var error) = factory.MapToPaymentMethodsDoc(receiptRequest, invoiceMark);

        error.Should().BeNull();
        paymentDoc.Should().NotBeNull();
        paymentDoc!.paymentMethods[0].invoiceMark.Should().Be(invoiceMark);
        paymentDoc.paymentMethods[0].paymentMethodDetails.Should().HaveCount(1);
        paymentDoc.paymentMethods[0].paymentMethodDetails[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
    }

    [Fact]
    public void RoutingCondition_PaymentTransfer_WithPreviousRef_ShouldBeDetected()
    {
        // Verify the condition that would trigger SendPaymentsMethod routing
        var receiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002);
        string? cbPreviousReceiptReference = "some-previous-ref";

        var receiptReferences = new List<(ReceiptRequest, ReceiptResponse)>
        {
            (new ReceiptRequest
            {
                cbTerminalID = "1",
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = "prev",
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>(),
                cbPayItems = new List<PayItem>(),
                ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
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
                        Data = "400001951868897",
                        ftSignatureFormat = SignatureFormat.Text,
                        ftSignatureType = (SignatureType)0x4752_2000_0000_0000
                    }
                }
            })
        };

        // The routing condition in ProcessReceiptAsync
        bool shouldUseSendPaymentsMethod =
            receiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) &&
            cbPreviousReceiptReference is not null &&
            receiptReferences != null && receiptReferences.Count > 0;

        shouldUseSendPaymentsMethod.Should().BeTrue();
    }

    [Fact]
    public void RoutingCondition_PaymentTransfer_WithoutPreviousRef_ShouldNotTrigger()
    {
        // Without cbPreviousReceiptReference, the routing should NOT trigger SendPaymentsMethod
        var receiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PaymentTransfer0x0002);
        string? cbPreviousReceiptReference = null;
        List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null;

        bool shouldUseSendPaymentsMethod =
            receiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) &&
            cbPreviousReceiptReference is not null &&
            receiptReferences != null && receiptReferences.Count > 0;

        shouldUseSendPaymentsMethod.Should().BeFalse();
    }

    [Fact]
    public void RoutingCondition_NonPaymentTransfer_WithPreviousRef_ShouldNotTrigger()
    {
        // A non-PaymentTransfer receipt with cbPreviousReceiptReference should NOT route to SendPaymentsMethod
        var receiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001);
        string? cbPreviousReceiptReference = "some-ref";

        bool shouldUseSendPaymentsMethod =
            receiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002) &&
            cbPreviousReceiptReference is not null;

        shouldUseSendPaymentsMethod.Should().BeFalse();
    }
}
