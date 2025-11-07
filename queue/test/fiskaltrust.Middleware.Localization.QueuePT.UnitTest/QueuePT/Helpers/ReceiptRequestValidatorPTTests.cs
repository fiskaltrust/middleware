using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Helpers;

public class ReceiptRequestValidatorPTTests
{
    [Fact]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenVoidFlagIsSet()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithFlag(ReceiptCaseFlags.Void),
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>()
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.VoidNotSupported);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenUserIsMissingOrEmpty(string user)
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = user,
            cbChargeItems = new List<ChargeItem>()
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_UserMissing);
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenUserIsNull()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = null,
            cbChargeItems = new List<ChargeItem>()
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_UserMissing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenChargeItemDescriptionIsMissing(string description)
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = description,
                    VATRate = 23m,
                    Amount = 10m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description is missing"));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenChargeItemDescriptionIsTooShort(string description)
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = description,
                    VATRate = 23m,
                    Amount = 10m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description must be longer than 3 characters"));
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenChargeItemVATRateIsNegative()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Valid Description",
                    VATRate = -1m,
                    Amount = 10m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "VAT rate is missing or invalid"));
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenChargeItemAmountIsZero()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Valid Description",
                    VATRate = 23m,
                    Amount = 0m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "amount (price) is missing or zero"));
    }

    [Theory]
    [InlineData("Garrafão 500ml")]
    [InlineData("Garrafa 75cl")]
    [InlineData("Frasco 0.5 litros")]
    [InlineData("Recipiente 0,25l")]
    [InlineData("Embalagem 330ml")]
    [InlineData("GARRAFÃO DE VINHO 500ML")]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenChargeItemIsBottleLessThanOneLiter(string description)
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = description,
                    VATRate = 23m,
                    Amount = 10m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "articles classified as 'Garrafão < 1 litro' are not allowed"));
    }

    [Theory]
    [InlineData("Garrafão 1.5 litros")]
    [InlineData("Garrafa 1000ml")]
    [InlineData("Vinho tinto")]
    [InlineData("Água mineral")]
    [InlineData("Produto normal")]
    [InlineData("Serviço de consultoria")]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenChargeItemIsValidBottleOrOtherProduct(string description)
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = description,
                    VATRate = 23m,
                    Amount = 10m
                }
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenAllValidationsPassed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Valid Product Description",
                    VATRate = 23m,
                    Amount = 10.50m
                },
                new ChargeItem
                {
                    Description = "Another Valid Product",
                    VATRate = 13m,
                    Amount = 5.25m
                }
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenNoChargeItems()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>()
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenChargeItemsIsNull()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = null
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldReportCorrectPosition_WhenMultipleChargeItemsAndSecondItemFails()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Valid Product Description",
                    VATRate = 23m,
                    Amount = 10.50m
                },
                new ChargeItem
                {
                    Description = "Bad", // Too short
                    VATRate = 13m,
                    Amount = 5.25m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(2, "description must be longer than 3 characters"));
    }
}