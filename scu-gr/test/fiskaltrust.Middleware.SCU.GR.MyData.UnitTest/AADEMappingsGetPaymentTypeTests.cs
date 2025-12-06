using System;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest.SCU.MyData;

public class AADEMappingsGetPaymentTypeTests
{
    [Theory]
    [InlineData(PayItemCase.UnknownPaymentType, MyDataPaymentMethods.Cash)]
    [InlineData(PayItemCase.CashPayment, MyDataPaymentMethods.Cash)]
    [InlineData(PayItemCase.CrossedCheque, MyDataPaymentMethods.Check)]
    [InlineData(PayItemCase.DebitCardPayment, MyDataPaymentMethods.PosEPos)]
    [InlineData(PayItemCase.CreditCardPayment, MyDataPaymentMethods.PosEPos)]
    [InlineData(PayItemCase.VoucherPaymentCouponVoucherByMoneyValue, MyDataPaymentMethods.Check)]
    [InlineData(PayItemCase.AccountsReceivable, MyDataPaymentMethods.OnCredit)]
    [InlineData(PayItemCase.OtherBankTransfer, MyDataPaymentMethods.ForeignPaymentsSpecialAccount)]
    public void GetPaymentType_WithValidPayItemCase_ReturnsCorrectPaymentType(PayItemCase payItemCase, int expectedPaymentType)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = "Test Payment",
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(payItemCase)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(expectedPaymentType);
    }

    [Theory]
    [InlineData(PayItemCase.NonCash)]
    [InlineData(PayItemCase.OnlinePayment)]
    [InlineData(PayItemCase.LoyaltyProgramCustomerCardPayment)]
    [InlineData(PayItemCase.TransferToCashbookVaultOwnerEmployee)]
    [InlineData(PayItemCase.InternalMaterialConsumption)]
    [InlineData(PayItemCase.Grant)]
    [InlineData(PayItemCase.TicketRestaurant)]
    public void GetPaymentType_WithUnsupportedPayItemCase_ReturnsMinusOne(PayItemCase payItemCase)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = "Test Payment",
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(payItemCase)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(-1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Regular transfer")]
    public void GetPaymentType_WithSEPATransferAndEmptyOrNullDescription_ReturnsDomesticPaymentAccount(string description)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = description,
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.SEPATransfer)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(MyDataPaymentMethods.DomesticPaymentAccount);
    }

    [Theory]
    [InlineData("iris")]
    [InlineData("IRIS")]
    public void GetPaymentType_WithSEPATransferAndIRISDescription_ReturnsIrisDirectPayments(string description)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = description,
            ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.SEPATransfer)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(MyDataPaymentMethods.IrisDirectPayments);
    }

    [Theory]
    [InlineData("RF code payment (Web banking)")]
    [InlineData("rf code payment (Web banking)")]
    public void GetPaymentType_WithSEPATransferAndWebBankingDescription_ReturnsWebBanking(string description)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = description,
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.SEPATransfer)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(MyDataPaymentMethods.WebBanking); // Falls through to default due to ToUpper() bug
    }

    [Theory]
    [InlineData("FOREIGN payment")]
    [InlineData("INTERNATIONAL transfer")]
    [InlineData("Some FOREIGN description")]
    [InlineData("INTERNATIONAL")]
    public void GetPaymentType_WithSEPATransferAndForeignDescription_ReturnsForeignPaymentsSpecialAccount(string description)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = description,
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.SEPATransfer)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(MyDataPaymentMethods.ForeignPaymentsSpecialAccount);
    }

    [Theory]
    [InlineData("some description")]
    [InlineData("Domestic payment")]
    [InlineData("Regular SEPA transfer")]
    public void GetPaymentType_WithSEPATransferAndRegularDescription_ReturnsDomesticPaymentAccount(string description)
    {
        // Arrange
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = description,
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.SEPATransfer)
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(MyDataPaymentMethods.DomesticPaymentAccount);
    }

    [Fact]
    public void GetPaymentType_WithUnknownPayItemCase_ThrowsException()
    {
        // Arrange - Use a cast to an unknown enum value that won't match any defined PayItemCase
        var unknownPayItemCase = (PayItemCase)0xFFFF; // A value that definitely doesn't exist in the enum
        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = "Test Payment",
            ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(unknownPayItemCase)
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => AADEMappings.GetPaymentType(payItem));
        exception.Message.Should().Contain("The Payment type");
        exception.Message.Should().Contain("is not supported");
    }

    [Fact]
    public void GetPaymentType_WithComplexPayItemCase_ExtractsCorrectCase()
    {
        // Arrange - Create a complex PayItemCase with flags and different parts
        var basePayItemCase = (PayItemCase)0x4752_2000_0000_0000;
        var payItemWithFlags = basePayItemCase
            .WithCase(PayItemCase.CashPayment)
            .WithFlag(PayItemCaseFlags.Tip); // Add a flag to ensure Case() method works correctly

        var payItem = new PayItem
        {
            Position = 1,
            Amount = 100.00m,
            Description = "Cash with tip",
            ftPayItemCase = payItemWithFlags
        };

        // Act
        var result = AADEMappings.GetPaymentType(payItem);

        // Assert
        result.Should().Be(MyDataPaymentMethods.Cash);
    }
}