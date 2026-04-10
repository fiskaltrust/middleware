using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class GetPaymentsTests
{
    private const long LocalPayItemFlag = 0x0000_0001_0000_0000;

    private static readonly ReceiptCase Pay0x3005Case =
        ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.Pay0x3005);

    private static PayItemCase GR(PayItemCase c) =>
        ((PayItemCase) 0x4752_2000_0000_0000).WithCase(c);

    private static PayItemCase WithLocalFlag(PayItemCase payItemCase) =>
        (PayItemCase) ((long) payItemCase | LocalPayItemFlag);

    private static AADEFactory CreateFactory() =>
        new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData { VatId = "EL123456789" }
        }, "https://test.receipts.example.com");

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

    private PaymentMethodDetailType[] GetPaymentDetails(List<PayItem> payItems, ReceiptCase? receiptCase = null)
    {
        var factory = CreateFactory();
        var request = BuildRequest(payItems);
        if (receiptCase.HasValue)
        {
            request.ftReceiptCase = receiptCase.Value;
        }
        (var doc, var error) = factory.MapToPaymentMethodsDoc(request, 123);
        error.Should().BeNull();
        return doc!.paymentMethods[0].paymentMethodDetails;
    }

    #region Payment Type Mapping

    [Fact]
    public void CashPayment_ShouldMapToCashType()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 15.0m, Description = "Cash Payment",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CashPayment)) }
        });

        details.Should().HaveCount(1);
        var detail = details[0];
        detail.type.Should().Be(MyDataPaymentMethods.Cash);
        detail.amount.Should().Be(15.0m);
        detail.paymentMethodInfo.Should().Be("Cash Payment");
        detail.tipAmountSpecified.Should().BeFalse();
        detail.ProvidersSignature.Should().BeNull();
    }

    [Fact]
    public void DebitCardPayment_ShouldMapToPosEPos()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 20.0m, Description = "Debit Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.DebitCardPayment)) }
        });

        details[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
    }

    [Fact]
    public void CreditCardPayment_ShouldMapToPosEPos()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 20.0m, Description = "Credit Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)) }
        });

        details[0].type.Should().Be(MyDataPaymentMethods.PosEPos);
    }

    [Fact]
    public void DescriptionShouldBeSetAsPaymentMethodInfo()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.0m, Description = "My Custom Description",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CashPayment)) }
        });

        details[0].paymentMethodInfo.Should().Be("My Custom Description");
    }

    #endregion

    #region Refund

    [Fact]
    public void RefundFlag_ShouldNegateAmount()
    {
        var details = GetPaymentDetails(
            new List<PayItem>
            {
                new PayItem { Position = 1, Amount = -10.0m, Description = "Refund",
                    ftPayItemCase = WithLocalFlag(GR(PayItemCase.CashPayment)).WithFlag(PayItemCaseFlags.Refund) }
            },
            Pay0x3005Case.WithFlag(ReceiptCaseFlags.Refund));

        details[0].amount.Should().Be(10.0m, "refund negates the negative amount to positive");
    }

    [Fact]
    public void RefundWithTip_ShouldNegateAmountAndSubtractTip()
    {
        var details = GetPaymentDetails(
            new List<PayItem>
            {
                new PayItem { Position = 1, Amount = -10.0m, Description = "Card",
                    ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)).WithFlag(PayItemCaseFlags.Refund) },
                new PayItem { Position = 2, Amount = 2.0m, Description = "Tip",
                    ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip).WithFlag(PayItemCaseFlags.Refund) }
            },
            Pay0x3005Case.WithFlag(ReceiptCaseFlags.Refund));

        details.Should().HaveCount(1);
        var detail = details[0];
        detail.amount.Should().Be(8.0m, "refund negates -10.0 to 10.0, then tip (2.0) is subtracted");
        detail.tipAmount.Should().Be(2.0m);
        detail.tipAmountSpecified.Should().BeTrue();
    }

    [Fact]
    public void RefundWithTip_PositiveRefundPayItem_ShouldAddTipForNegativeAmount()
    {
        var details = GetPaymentDetails(
            new List<PayItem>
            {
                new PayItem { Position = 1, Amount = -10.0m, Description = "Card",
                    ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)).WithFlag(PayItemCaseFlags.Refund) },
                new PayItem { Position = 2, Amount = 1.8m, Description = "Tip",
                    ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip).WithFlag(PayItemCaseFlags.Refund) }
            },
            Pay0x3005Case.WithFlag(ReceiptCaseFlags.Refund));

        details.Should().HaveCount(1);
        var detail = details[0];
        detail.amount.Should().Be(8.2m, "refund negates +10.0 to -10.0, then tip (1.8) should be added");
        detail.tipAmount.Should().Be(1.8m);
        detail.tipAmountSpecified.Should().BeTrue();
    }

    #endregion

    #region Tip Handling

    [Fact]
    public void TipNextInLine_ShouldSubtractFromAmountAndSetTipAmount()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)) },
            new PayItem { Position = 2, Amount = -1.8m, Description = "Tip",
                ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        details.Should().HaveCount(1);
        var detail = details[0];
        detail.amount.Should().Be(8.2m, "10.0 - 1.8 tip");
        detail.tipAmount.Should().Be(1.8m);
        detail.tipAmountSpecified.Should().BeTrue();
    }

    [Fact]
    public void TipWithDifferentCase_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.0m, Description = "Cash",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CashPayment)) },
            new PayItem { Position = 2, Amount = -1.0m, Description = "Tip",
                ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(request, 123);

        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Exception.Should().BeOfType<ArgumentException>();
        error.Exception.Message.Should().Contain("does not match");
    }

    [Fact]
    public void TipNotDirectlyAfterPayment_ShouldReturnError()
    {
        var factory = CreateFactory();
        var request = BuildRequest(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)) },
            new PayItem { Position = 2, Amount = 5.0m, Description = "Cash",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CashPayment)) },
            new PayItem { Position = 3, Amount = -1.0m, Description = "Card Tip",
                ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        (var doc, var error) = factory.MapToPaymentMethodsDoc(request, 123);

        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Exception.Should().BeOfType<ArgumentException>();
        error.Exception.Message.Should().Contain("does not match");
    }

    [Fact]
    public void MultiplePaymentTypes_TipOnlyAppliestoMatchingPrecedingPayment()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 5.0m, Description = "Cash",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CashPayment)) },
            new PayItem { Position = 2, Amount = 15.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)) },
            new PayItem { Position = 3, Amount = -1.0m, Description = "Card Tip",
                ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip) }
        });

        details.Should().HaveCount(2);
        details[0].type.Should().Be(MyDataPaymentMethods.Cash);
        details[0].amount.Should().Be(5.0m);
        details[0].tipAmountSpecified.Should().BeFalse();

        details[1].type.Should().Be(MyDataPaymentMethods.PosEPos);
        details[1].amount.Should().Be(14.0m, "15.0 - 1.0 tip");
        details[1].tipAmount.Should().Be(1.0m);
        details[1].tipAmountSpecified.Should().BeTrue();
    }

    #endregion

    #region Provider Signature Data

    [Fact]
    public void NoPayItemCaseData_ShouldNotSetProviderSignature()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 10.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.DebitCardPayment)) }
        });

        details[0].ProvidersSignature.Should().BeNull();
        details[0].transactionId.Should().BeNull();
    }

    [Fact]
    public void CloudApiProviderData_ShouldExtractSignatureAndTransactionId()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem
            {
                Position = 1, Amount = 10.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.DebitCardPayment)),
                ftPayItemCaseData = new
                {
                    Provider = new
                    {
                        Protocol = "",
                        ProtocolVersion = "1.0",
                        Action = "",
                        ProtocolRequest = new
                        {
                            aadeProviderSignatureData = "",
                            aadeProviderSignature = "cloud-signature-123"
                        },
                        ProtocolResponse = new
                        {
                            aadeTransactionId = "TXN-CLOUD-001"
                        }
                    }
                }
            }
        });

        var detail = details[0];
        detail.ProvidersSignature.Should().NotBeNull();
        detail.ProvidersSignature!.Signature.Should().Be("cloud-signature-123");
        detail.ProvidersSignature.SigningAuthor.Should().Be("126");
        detail.transactionId.Should().Be("TXN-CLOUD-001");
    }

    [Fact]
    public void GenericAadeSignatureData_ShouldExtractSignatureAndTransactionId()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem
            {
                Position = 1, Amount = 10.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.DebitCardPayment)),
                ftPayItemCaseData = new
                {
                    aadeSignatureData = new
                    {
                        aadeProviderSignature = "generic-signature-456",
                        aadeTransactionId = "TXN-GENERIC-002"
                    }
                }
            }
        });

        var detail = details[0];
        detail.ProvidersSignature.Should().NotBeNull();
        detail.ProvidersSignature!.Signature.Should().Be("generic-signature-456");
        detail.ProvidersSignature.SigningAuthor.Should().Be("126");
        detail.transactionId.Should().Be("TXN-GENERIC-002");
    }

    #endregion

    #region Combined Tip + Provider Data

    [Fact]
    public void TipWithProviderData_ShouldApplyBoth()
    {
        var details = GetPaymentDetails(new List<PayItem>
        {
            new PayItem
            {
                Position = 1, Amount = 10.0m, Description = "Card",
                ftPayItemCase = WithLocalFlag(GR(PayItemCase.CreditCardPayment)),
                ftPayItemCaseData = new
                {
                    aadeSignatureData = new
                    {
                        aadeProviderSignature = "sig-with-tip",
                        aadeTransactionId = "TXN-TIP-001"
                    }
                }
            },
            new PayItem
            {
                Position = 2, Amount = -1.5m, Description = "Tip",
                ftPayItemCase = GR(PayItemCase.CreditCardPayment).WithFlag(PayItemCaseFlags.Tip)
            }
        });

        details.Should().HaveCount(1);
        var detail = details[0];
        detail.amount.Should().Be(8.5m, "10.0 - 1.5 tip");
        detail.tipAmount.Should().Be(1.5m);
        detail.tipAmountSpecified.Should().BeTrue();
        detail.ProvidersSignature!.Signature.Should().Be("sig-with-tip");
        detail.transactionId.Should().Be("TXN-TIP-001");
    }

    #endregion
}
