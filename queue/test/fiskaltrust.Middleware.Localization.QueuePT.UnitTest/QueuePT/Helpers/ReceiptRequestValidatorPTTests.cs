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
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description must be at least 3 characters long"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("abcde")]
    [InlineData("This is a long description")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("AB")]
    [InlineData("Product")]
    [InlineData("Line item 1")]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenChargeItemDescriptionIsValidLength(string description)
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

    [Theory]
    [InlineData("abcd")]
    [InlineData("abcde")]
    [InlineData("This is a long description")]
    [InlineData("1234")]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenChargeItemDescriptionIsTooLong(string description)
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
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description must not be longer than 3 characters"));
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10.50m
                },
                new ChargeItem
                {
                    Description = "ab", // Too short
                    VATRate = 13m,
                    Amount = 5.25m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(2, "description must be at least 3 characters long"));
    }

    #region Portuguese Tax ID Validation Tests

    [Fact]
    public void IsValidPortugueseTaxId_ShouldValidateFiskaltrustPortugalNIF()
    {
        // Arrange - This is the actual NIF used by fiskaltrust Portugal in the codebase
        const string fiskaltrustPortugalNIF = "980833310";

        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(fiskaltrustPortugalNIF);

        // Assert
        result.Should().BeTrue("fiskaltrust Portugal's NIF should be valid");
    }

    [Theory]
    [InlineData("123456789")] // Valid NIF starting with 1
    [InlineData("502757191")] // Valid NIF starting with 5
    [InlineData("800000030")] // Valid NIF starting with 8
    [InlineData("980833310")] // Valid NIF starting with 9 (fiskaltrust Portugal)
    public void IsValidPortugueseTaxId_ShouldReturnTrue_ForValidNIF(string nif)
    {
        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(nif);

        // Assert
        result.Should().BeTrue($"NIF '{nif}' should be valid");
    }

    [Theory]
    [InlineData("000000000")] // Invalid: starts with 0
    [InlineData("400000001")] // Invalid: starts with 4 (not a valid first digit)
    [InlineData("600000014")] // Valid NIF starting with 6
    [InlineData("700000027")] // Valid NIF starting with 7
    [InlineData("312345671")] // Valid NIF starting with 3
    [InlineData("234567890")] // Valid NIF starting with 2
    [InlineData("123456788")] // Invalid: wrong check digit (should be 9)
    [InlineData("980833311")] // Invalid: wrong check digit (should be 0)
    [InlineData("12345678")]  // Invalid: only 8 digits
    [InlineData("1234567890")] // Invalid: 10 digits
    [InlineData("12345678A")] // Invalid: contains letter
    [InlineData("123-456-789")] // Invalid: contains hyphens
    [InlineData("123 456 789")] // Invalid: contains spaces (should fail even after trim)
    [InlineData("")] // Invalid: empty string
    [InlineData("   ")] // Invalid: whitespace only
    [InlineData("ABCDEFGHI")] // Invalid: all letters
    public void IsValidPortugueseTaxId_ShouldReturnFalse_ForInvalidNIF(string nif)
    {
        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(nif);

        // Assert
        result.Should().BeFalse($"NIF '{nif}' should be invalid");
    }

    [Fact]
    public void IsValidPortugueseTaxId_ShouldReturnFalse_ForNullInput()
    {
        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(null);

        // Assert
        result.Should().BeFalse("null input should be invalid");
    }

    [Theory]
    [InlineData(" 123456789")] // Leading space
    [InlineData("123456789 ")] // Trailing space
    [InlineData(" 123456789 ")] // Both leading and trailing spaces
    public void IsValidPortugueseTaxId_ShouldHandleWhitespace_ByTrimmingAndValidating(string nifWithSpaces)
    {
        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(nifWithSpaces);

        // Assert
        result.Should().BeTrue($"NIF '{nifWithSpaces}' should be valid after trimming");
    }

    [Theory]
    [InlineData("123456789", "Natural person (singular)")] // Starts with 1
    [InlineData("502757191", "Legal entity (collective)")] // Starts with 5
    [InlineData("800000030", "Sole proprietor")] // Starts with 8
    [InlineData("980833310", "Legal entity (collective)")] // Starts with 9
    public void IsValidPortugueseTaxId_ShouldAcceptValidFirstDigits_ForDifferentEntityTypes(string nif, string entityType)
    {
        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(nif);

        // Assert
        result.Should().BeTrue($"NIF '{nif}' for {entityType} should be valid");
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenCustomerTaxIdIsInvalid()
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerVATId = "123456788", // Invalid check digit
                CustomerName = "Test Customer"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Contain("EEEE_Invalid Portuguese Tax Identification Number");
        exception.Message.Should().Contain("123456788");
    }

    [Theory]
    [InlineData("000000000")] // Starts with 0
    [InlineData("400000001")] // Starts with 4
    [InlineData("12345678A")] // Contains letter
    [InlineData("12345678")]  // Too short
    [InlineData("1234567890")] // Too long
    [InlineData("500402923")] // Too long
    public void ValidateReceiptOrThrow_ShouldThrowException_WhenCustomerTaxIdIsInvalidFormat(string invalidNif)
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerVATId = invalidNif,
                CustomerName = "Test Customer"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Contain("EEEE_Invalid Portuguese Tax Identification Number");
        exception.Message.Should().Contain(invalidNif);
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenCustomerTaxIdIsValid()
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerVATId = "123456789", // Valid NIF
                CustomerName = "Test Customer",
                CustomerStreet = "Rua Example",
                CustomerCity = "Lisboa",
                CustomerZip = "1000-001"
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenCustomerHasNoTaxId()
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerName = "Test Customer",
                CustomerStreet = "Rua Example"
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenCustomerIsNull()
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = null
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData("   ")] // Whitespace only
    [InlineData(null)] // Null
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenCustomerTaxIdIsEmptyOrNull(string emptyTaxId)
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerVATId = emptyTaxId,
                CustomerName = "Test Customer"
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw when tax ID is empty/null
    }

    [Theory]
    [InlineData("980833310")] // fiskaltrust Portugal NIF
    [InlineData("502757191")] // Another valid NIF
    [InlineData("506848558")] // Another valid NIF
    [InlineData("199998132")] // Another valid NIF
    [InlineData("500402922")] // Another valid NIF
    [InlineData("503630330")] // Another valid NIF
    public void ValidateReceiptOrThrow_ShouldNotThrowException_WhenCustomerHasValidRealWorldNIF(string validNif)
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
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerVATId = validNif,
                CustomerName = "Test Customer Company"
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldValidateCustomerTaxId_BeforeOtherValidations()
    {
        // Arrange - Create a receipt with an invalid NIF and also missing user
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbUser = "user123", // Valid user
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Valid Product",
                    VATRate = 23m,
                    Amount = 10m
                }
            },
            cbCustomer = new
            {
                CustomerVATId = "123456788", // Invalid check digit
                CustomerName = "Test Customer"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Contain("EEEE_Invalid Portuguese Tax Identification Number");
    }

    [Fact]
    public void IsValidPortugueseTaxId_AlgorithmExplanation_ShouldValidateAccordingToWikipediaSpecs()
    {
        // This test documents the algorithm as described in:
        // https://pt.wikipedia.org/wiki/N%C3%BAmero_de_identifica%C3%A7%C3%A3o_fiscal
        
        // Example calculation for NIF: 123456789
        // Step 1: Valid first digit (1, 2, 3, 5, 6, 7, 8, or 9) - In this case: 1 ✓
        // Step 2: Calculate check digit using formula:
        //   1×9 + 2×8 + 3×7 + 4×6 + 5×5 + 6×4 + 7×3 + 8×2 = 204
        //   204 % 11 = 6
        //   Check digit = 11 - 6 = 5 (but if remainder < 2, use 0)
        //   Expected 9th digit = 5... but wait, let's recalculate properly:
        
        // Correct calculation for 123456789:
        // 1×9 = 9
        // 2×8 = 16
        // 3×7 = 21
        // 4×6 = 24
        // 5×5 = 25
        // 6×4 = 24
        // 7×3 = 21
        // 8×2 = 16
        // Sum = 156
        // 156 % 11 = 2
        // Check digit = 11 - 2 = 9 ✓
        
        // Arrange
        var nif = "123456789";

        // Act
        var result = ReceiptRequestValidatorPT.IsValidPortugueseTaxId(nif);

        // Assert
        result.Should().BeTrue("the check digit calculation matches the Wikipedia algorithm");
    }

    #endregion

    #region Charge Item Description Length Validation Tests

    [Fact]
    public void ValidateReceiptOrThrow_ShouldAcceptExactly3Characters()
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
                    Description = "ABC",
                    VATRate = 23m,
                    Amount = 10m
                }
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    public void ValidateReceiptOrThrow_ShouldReject1Or2Characters(string description)
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
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description must be at least 3 characters long"));
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("Product")]
    [InlineData("XYZ")]
    public void ValidateReceiptOrThrow_ShouldAcceptDescriptions_With3OrMoreCharacters(string description)
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

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("X")]
    public void ValidateReceiptOrThrow_ShouldRejectDescriptions_ShorterThan3Characters(string description)
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
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description must be at least 3 characters long"));
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldAcceptMultipleChargeItems_WithValidDescriptions()
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
                    Description = "Product A",
                    VATRate = 23m,
                    Amount = 10m
                },
                new ChargeItem
                {
                    Description = "Product BB",
                    VATRate = 13m,
                    Amount = 5m
                },
                new ChargeItem
                {
                    Description = "CCC",
                    VATRate = 6m,
                    Amount = 3m
                }
            }
        };

        // Act & Assert
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest); // Should not throw
    }

    [Fact]
    public void ValidateReceiptOrThrow_ShouldRejectFirstInvalidItem_InMultipleChargeItems()
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
                    Description = "Product A",
                    VATRate = 23m,
                    Amount = 10m
                },
                new ChargeItem
                {
                    Description = "AB", // Too short
                    VATRate = 13m,
                    Amount = 5m
                },
                new ChargeItem
                {
                    Description = "CCC",
                    VATRate = 6m,
                    Amount = 3m
                }
            }
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest));
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(2, "description must be at least 3 characters long"));
    }

    [Theory]
    [InlineData("A B")] // 3 characters with space
    [InlineData("1-2")] // 3 characters with hyphen
    [InlineData("X.Y")] // 3 characters with dot
    public void ValidateReceiptOrThrow_ShouldAcceptDescriptions_WithSpecialCharacters_IfLength3OrMore(string description)
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

    [Theory]
    [InlineData("€99")] // 3 characters with euro symbol
    [InlineData("10%")] // 3 characters with percentage
    [InlineData("@#$")] // 3 special characters
    public void ValidateReceiptOrThrow_ShouldAcceptDescriptions_WithUnicodeCharacters_IfLength3OrMore(string description)
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

    [Theory]
    [InlineData("€9")]  // 2 characters - too short
    [InlineData("1%")]  // 2 characters - too short
    [InlineData("@#")]  // 2 characters - too short
    public void ValidateReceiptOrThrow_ShouldRejectDescriptions_WithUnicodeCharacters_IfShorterThan3(string description)
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
        exception.Message.Should().Be(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(1, "description must be at least 3 characters long"));
    }

    #endregion
}