using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using System.Reflection;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTests.Factories;

public class PortugalReceiptCalculationsTests
{
    private static (string documentType, string uniqueIdentification) CallExtractDocumentTypeAndUniqueIdentification(string ftReceiptIdentification)
    {
        // Use reflection to access the private method
        var method = typeof(PortugalReceiptCalculations).GetMethod(
            "ExtractDocumentTypeAndUniqueIdentification",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("Method ExtractDocumentTypeAndUniqueIdentification not found");
        }

        return ((string, string))method.Invoke(null, new object[] { ftReceiptIdentification })!;
    }

    #region ExtractDocumentTypeAndUniqueIdentification Tests

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenValidInputWithHashAndSpace()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#FT 20241210001";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FT");
        uniqueIdentification.Should().Be("FT 20241210001");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenValidInputWithoutHash()
    {
        // Arrange
        var ftReceiptIdentification = "FS 20241210002";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FS");
        uniqueIdentification.Should().Be("FS 20241210002");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenMultipleHashSymbols()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#another#section#NC 20241210003";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("NC");
        uniqueIdentification.Should().Be("NC 20241210003");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenNoSpaceInLocalPart()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#FT20241210004";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be("FT20241210004");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenSpaceIsAtEnd()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#FT20241210005 ";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be("FT20241210005 ");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenSpaceIsAtBeginning()
    {
        // Arrange
        var ftReceiptIdentification = "prefix# 20241210006";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be(" 20241210006");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenMultipleSpaces()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#FT 20241210 007";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FT");
        uniqueIdentification.Should().Be("FT 20241210 007");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyValues_WhenInputIsEmpty()
    {
        // Arrange
        var ftReceiptIdentification = "";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be(string.Empty);
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyValues_WhenInputIsNull()
    {
        // Arrange
        string ftReceiptIdentification = null!;

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be(string.Empty);
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenOnlyHashSymbol()
    {
        // Arrange
        var ftReceiptIdentification = "#";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be("");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenHashWithSpace()
    {
        // Arrange
        var ftReceiptIdentification = "# ";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be(" ");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenHashWithValidDocumentTypeAndId()
    {
        // Arrange
        var ftReceiptIdentification = "#FR 123456";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FR");
        uniqueIdentification.Should().Be("FR 123456");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenLongDocumentType()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#LONGDOCTYPE 20241210008";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("LONGDOCTYPE");
        uniqueIdentification.Should().Be("LONGDOCTYPE 20241210008");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenSpecialCharactersInDocumentType()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#FT-1 20241210009";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FT-1");
        uniqueIdentification.Should().Be("FT-1 20241210009");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenSpecialCharactersInUniqueId()
    {
        // Arrange
        var ftReceiptIdentification = "ft5B1#FT 123434/1";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FT");
        uniqueIdentification.Should().Be("FT 123434/1");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenRealWorldExample1()
    {
        // Arrange - Simulating a real-world invoice receipt identification with actual format
        var ftReceiptIdentification = "ft5B1#FT 20241210001";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FT");
        uniqueIdentification.Should().Be("FT 20241210001");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenRealWorldExample2()
    {
        // Arrange - Simulating a real-world credit note receipt identification with actual format
        var ftReceiptIdentification = "ft5B1#NC 20241210002";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("NC");
        uniqueIdentification.Should().Be("NC 20241210002");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnCorrectValues_WhenRealWorldExample3()
    {
        // Arrange - Simulating a real-world simplified invoice receipt identification with actual format
        var ftReceiptIdentification = "ft5B1#FS 20241210003";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FS");
        uniqueIdentification.Should().Be("FS 20241210003");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenOnlySpaceAfterHash()
    {
        // Arrange
        var ftReceiptIdentification = "prefix# ";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be(" ");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldReturnEmptyDocumentType_WhenEmptyAfterHash()
    {
        // Arrange
        var ftReceiptIdentification = "prefix#";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be(string.Empty);
        uniqueIdentification.Should().Be("");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldHandleComplexNumbering_WhenActualFormat()
    {
        // Arrange - Testing with complex numbering like the actual format
        var ftReceiptIdentification = "ft5B1#FT 20241210001/5";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("FT");
        uniqueIdentification.Should().Be("FT 20241210001/5");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldHandleLongNumbers_WhenActualFormat()
    {
        // Arrange - Testing with longer numbers as seen in production
        var ftReceiptIdentification = "ft5B1#NC 202412100000001/999";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("NC");
        uniqueIdentification.Should().Be("NC 202412100000001/999");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldHandleMinimalValidFormat()
    {
        // Arrange - Testing minimal valid format
        var ftReceiptIdentification = "ft5B1#A 1";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("A");
        uniqueIdentification.Should().Be("A 1");
    }

    [Fact]
    public void ExtractDocumentTypeAndUniqueIdentification_ShouldHandleQueueIdVariations()
    {
        // Arrange - Testing different queue ID variations but same document format
        var ftReceiptIdentification = "ft1A3#GR 20241215001/1";

        // Act
        var (documentType, uniqueIdentification) = CallExtractDocumentTypeAndUniqueIdentification(ftReceiptIdentification);

        // Assert
        documentType.Should().Be("GR");
        uniqueIdentification.Should().Be("GR 20241215001/1");
    }

    #endregion

    #region QR Code Generation Tests

    private static ReceiptRequest CreateTestReceiptRequest(string ftReceiptCase = "0x0001")
    {
        return new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)Convert.ToInt64(ftReceiptCase, 16),
            cbReceiptMoment = new DateTime(2024, 12, 10, 14, 30, 0),
            cbChargeItems = [
            
                new ChargeItem
                {
                    Position = 1,
                    Description = "Test Product",
                    Amount = 100.00m,
                    VATAmount = 23.00m,
                    VATRate = 23.00m,
                    Quantity = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate
                },
                new ChargeItem
                {
                    Position = 2,
                    Description = "Reduced VAT Product",
                    Amount = 50.00m,
                    VATAmount = 3.00m,
                    VATRate = 6.00m,
                    Quantity = 1,
                    ftChargeItemCase = ChargeItemCase.DiscountedVatRate1
                }
            ],
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "123456789",
                CustomerCountry = "PT"
            }
        };
    }

    private static ReceiptResponse CreateTestReceiptResponse(string ftReceiptIdentification)
    {
        return new ReceiptResponse
        {
            ftReceiptIdentification = ftReceiptIdentification,
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptMoment = new DateTime(2024, 12, 10, 14, 30, 0)
        };
    }

    [Fact]
    public void CreateCreditNoteQRCode_ShouldGenerateQRCodeWithCorrectUniqueIdentification()
    {
        // Arrange
        var qrCodeHash = "TESTHASH123";
        var issuerTIN = "123456789";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft5B1#NC 20241210001");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("A:123456789*"); // IssuerTIN
        qrCode.Should().Contain("D:NC*"); // DocumentType extracted from ftReceiptIdentification
        qrCode.Should().Contain("G:NC 20241210001*"); // UniqueIdentificationOfTheDocument - everything after #
        qrCode.Should().Contain("H:0*"); // ATCUD
        qrCode.Should().Contain("Q:TESTHASH123*"); // Hash
        qrCode.Should().Contain("R:9999*"); // SoftwareCertificateNumber
        qrCode.Should().Contain($"S:qiid={response.ftQueueItemID}"); // OtherInformation
    }

    [Fact]
    public void CreateInvoiceQRCode_ShouldGenerateQRCodeWithCorrectUniqueIdentification()
    {
        // Arrange
        var qrCodeHash = "INVHASH456";
        var issuerTIN = "987654321";
        var atcud = "123";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft7A2#FT 20241210002/1");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("A:987654321*"); // IssuerTIN
        qrCode.Should().Contain("D:FT*"); // DocumentType extracted from ftReceiptIdentification
        qrCode.Should().Contain("G:FT 20241210002/1*"); // UniqueIdentificationOfTheDocument - everything after #
        qrCode.Should().Contain("H:123*"); // ATCUD
        qrCode.Should().Contain("I1:PT*"); // TaxCountryRegion
        qrCode.Should().Contain("Q:INVHASH456*"); // Hash
    }

    [Fact]
    public void CreateProFormaQRCode_ShouldGenerateQRCodeWithCorrectUniqueIdentification()
    {
        // Arrange
        var qrCodeHash = "PROFHASH789";
        var issuerTIN = "555666777";
        var atcud = "456";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft3X4#PF 20241210003");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateProFormaQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("A:555666777*"); // IssuerTIN
        qrCode.Should().Contain("D:PF*"); // DocumentType extracted from ftReceiptIdentification
        qrCode.Should().Contain("G:PF 20241210003*"); // UniqueIdentificationOfTheDocument - everything after #
        qrCode.Should().Contain("H:456*"); // ATCUD
        qrCode.Should().Contain("I1:PT*"); // TaxCountryRegion
        qrCode.Should().Contain("Q:PROFHASH789*"); // Hash
    }

    [Fact]
    public void CreateRGQRCode_ShouldGenerateQRCodeWithCorrectUniqueIdentification()
    {
        // Arrange
        var qrCodeHash = "RGHASH012";
        var issuerTIN = "111222333";
        var atcud = "789";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft8Y9#RG 20241210004");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateRGQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("A:111222333*"); // IssuerTIN
        qrCode.Should().Contain("D:RG*"); // DocumentType extracted from ftReceiptIdentification
        qrCode.Should().Contain("G:RG 20241210004*"); // UniqueIdentificationOfTheDocument - everything after #
        qrCode.Should().Contain("H:789*"); // ATCUD
        qrCode.Should().Contain("I1:0*"); // TaxCountryRegion = "0" for RG documents
        qrCode.Should().Contain("Q:RGHASH012*"); // Hash
    }

    [Fact]
    public void CreateSimplifiedInvoiceQRCode_ShouldGenerateQRCodeWithCorrectUniqueIdentification()
    {
        // Arrange
        var qrCodeHash = "SIMPHASH345";
        var issuerTIN = "444555666";
        var atcud = "012";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft2B3#FS 20241210005/99");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("A:444555666*"); // IssuerTIN
        qrCode.Should().Contain("D:FS*"); // DocumentType extracted from ftReceiptIdentification
        qrCode.Should().Contain("G:FS 20241210005/99*"); // UniqueIdentificationOfTheDocument - everything after #
        qrCode.Should().Contain("H:012*"); // ATCUD
        qrCode.Should().Contain("I1:PT*"); // TaxCountryRegion
        qrCode.Should().Contain("Q:SIMPHASH345*"); // Hash
    }

    [Fact]
    public void QRCodeGeneration_ShouldHandleComplexReceiptIdentification()
    {
        // Arrange
        var qrCodeHash = "COMPLEXHASH";
        var issuerTIN = "777888999";
        var atcud = "999";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft5B1#queue#system#NC 202412100000001/999");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("D:NC*"); // DocumentType should be extracted correctly from complex ID
        qrCode.Should().Contain("G:NC 202412100000001/999*"); // UniqueIdentificationOfTheDocument should be everything after last #
    }

    [Fact]
    public void QRCodeGeneration_ShouldHandleNoSpaceInReceiptIdentification()
    {
        // Arrange
        var qrCodeHash = "NOSPACE";
        var issuerTIN = "111111111";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft5B1#FS20241210006");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("D:*"); // DocumentType should be empty when no space found
        qrCode.Should().Contain("G:FS20241210006*"); // UniqueIdentificationOfTheDocument should still be everything after #
    }

    [Fact]
    public void QRCodeGeneration_ShouldIncludeCorrectVATAmounts()
    {
        // Arrange
        var qrCodeHash = "VATHASH";
        var issuerTIN = "123123123";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse("ft5B1#FT 20241210007");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("I3:47.00*"); // TaxableBasisOfVAT_ReducedRate (50 - 3)
        qrCode.Should().Contain("I4:3.00*"); // TotalVAT_ReducedRate
        qrCode.Should().Contain("I7:77.00*"); // TaxableBasisOfVAT_StandardRate (100 - 23)
        qrCode.Should().Contain("I8:23.00*"); // TotalVAT_StandardRate
        qrCode.Should().Contain("N:26.00*"); // TotalTaxes (23 + 3)
        qrCode.Should().Contain("O:150.00*"); // GrossTotal (100 + 50)
    }

    [Fact]
    public void QRCodeGeneration_ShouldIncludeCustomerInfo()
    {
        // Arrange
        var qrCodeHash = "CUSTHASH";
        var issuerTIN = "999999999";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        request.cbCustomer = new MiddlewareCustomer
        {
            CustomerVATId = "987654321",
            CustomerCountry = "ES"
        };
        var response = CreateTestReceiptResponse("ft5B1#FT 20241210008");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("B:987654321*"); // CustomerTIN
        qrCode.Should().Contain("C:ES*"); // CustomerCountry
    }

    [Fact]
    public void QRCodeGeneration_ShouldUseAnonymousCustomerWhenNoCustomerProvided()
    {
        // Arrange
        var qrCodeHash = "ANONHASH";
        var issuerTIN = "888888888";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        request.cbCustomer = null; // No customer provided
        var response = CreateTestReceiptResponse("ft5B1#FS 20241210009");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("B:999999990*"); // Anonymous CustomerTIN
        qrCode.Should().Contain("C:Desconhecido*"); // Anonymous CustomerCountry
    }

    [Fact]
    public void QRCodeGeneration_ShouldIncludeCorrectDocumentDate()
    {
        // Arrange
        var qrCodeHash = "DATEHASH";
        var issuerTIN = "777777777";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        request.cbReceiptMoment = new DateTime(2024, 12, 25, 10, 15, 30);
        var response = CreateTestReceiptResponse("ft5B1#FT 20241225010");

        // Act
        var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain("F:20241225*"); // DocumentDate in YYYYMMDD format
    }

    [Theory]
    [InlineData("ft5B1#FT 123", "FT", "FT 123")]
    [InlineData("ft5B1#NC 456/1", "NC", "NC 456/1")]
    [InlineData("ft5B1#FS 789abc", "FS", "FS 789abc")]
    [InlineData("ft5B1#PF 2024", "PF", "PF 2024")]
    [InlineData("ft5B1#RG 555", "RG", "RG 555")]
    [InlineData("ft5B1#CUSTOM 999", "CUSTOM", "CUSTOM 999")]
    [InlineData("ft5B1#A B", "A", "A B")]
    [InlineData("ft5B1#NoSpace", "", "NoSpace")]
    [InlineData("ft5B1# ", "", " ")]
    [InlineData("ft5B1#", "", "")]
    public void QRCodeGeneration_ShouldExtractDocumentTypeAndUniqueIdCorrectly(string ftReceiptIdentification, string expectedDocumentType, string expectedUniqueId)
    {
        // Arrange
        var qrCodeHash = "EXTRACTHASH";
        var issuerTIN = "123456789";
        var atcud = "0";
        var request = CreateTestReceiptRequest();
        var response = CreateTestReceiptResponse(ftReceiptIdentification);

        // Act
        var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(qrCodeHash, issuerTIN, atcud, request, response);

        // Assert
        qrCode.Should().NotBeNullOrEmpty();
        qrCode.Should().Contain($"D:{expectedDocumentType}*"); // DocumentType
        qrCode.Should().Contain($"G:{expectedUniqueId}*"); // UniqueIdentificationOfTheDocument
    }

    #endregion
}