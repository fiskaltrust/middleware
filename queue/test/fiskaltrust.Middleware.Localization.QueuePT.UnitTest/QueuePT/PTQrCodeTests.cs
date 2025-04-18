using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using System;

namespace fiskaltrust.Middleware.Localization.QueuePT.Tests;

public class PTQrCodeTests
{
    private PTQrCode _qrCode => new PTQrCode
    {
        IssuerTIN = "123456789",
        CustomerTIN = PTQrCode.CUSTOMER_TIN_ANONYMOUS,
        CustomerCountry = PTQrCode.CUSTOMER_COUNTRY_ANONYMOUS,
        DocumentType = "FT",
        DocumentStatus = "N",
        DocumentDate = new DateTime(2024, 10, 8),
        UniqueIdentificationOfTheDocument = "FT20241008001",
        ATCUD = "0",
        TaxCountryRegion = "PT",
        TotalTaxes = 23.34234m,
        GrossTotal = 100.34234m,
        Hash = "HASH1234",
        SoftwareCertificateNumber = "CERT12345",
        OtherInformation = "Payment via IBAN;ATM Ref. 123456"
    };

    [Fact]
    public void GenerateQRCode_ShouldIncludeMandatoryFields()
    {
        // Expected Output for mandatory fields
        var expected = "A:123456789*"
                     + "B:999999990*"
                     + "C:PT*"
                     + "D:FT*"
                     + "E:N*"
                     + "F:20241008*"
                     + "G:FT20241008001*"
                     + "H:0*"
                     + "I1:PT*"
                     + "N:23.34*"
                     + "O:100.34*"
                     + "Q:HASH1234*"
                     + "R:CERT12345*"
                     + "S:Payment via IBAN;ATM Ref. 123456";

        // Actual Output
        var actual = _qrCode.GenerateQRCode();

        // Assert using FluentAssertions
        actual.Should().Be(expected);
    }

    [Fact]
    public void GenerateQRCode_ShouldIncludeVATFields_WhenProvided()
    {
        // Setup: Adding VAT fields
        var qrCode = _qrCode;
        qrCode.TaxableBasisOfVAT_StandardRate = 80.00m;
        qrCode.TotalVAT_StandardRate = 20.00m;

        // Expected Output including VAT fields
        var expected = "A:123456789*"
                     + "B:999999990*"
                     + "C:PT*"
                     + "D:FT*"
                     + "E:N*"
                     + "F:20241008*"
                     + "G:FT20241008001*"
                     + "H:0*"
                     + "I1:PT*"
                     + "I7:80.00*"
                     + "I8:20.00*"
                     + "N:23.34*"
                     + "O:100.34*"
                     + "Q:HASH1234*"
                     + "R:CERT12345*"
                     + "S:Payment via IBAN;ATM Ref. 123456";

        // Actual Output
        var actual = qrCode.GenerateQRCode();

        // Assert using FluentAssertions
        actual.Should().Be(expected);
    }

    [Fact]
    public void GenerateQRCode_ShouldNotIncludeOptionalFields_WhenNull()
    {
        var qrCode = _qrCode;
        // Setup: Removing the optional "OtherInformation" field
        qrCode.OtherInformation = null;

        // Expected Output without optional field
        var expected = "A:123456789*"
                     + "B:999999990*"
                     + "C:PT*"
                     + "D:FT*"
                     + "E:N*"
                     + "F:20241008*"
                     + "G:FT20241008001*"
                     + "H:0*"
                     + "I1:PT*"
                     + "N:23.34*"
                     + "O:100.34*"
                     + "Q:HASH1234*"
                     + "R:CERT12345";

        // Actual Output
        var actual = qrCode.GenerateQRCode();

        // Assert using FluentAssertions
        actual.Should().Be(expected);
    }

    [Fact]
    public void GenerateQRCode_ShouldFormatDecimalFieldsCorrectly()
    {
        var qrCode = _qrCode;
        // Setup: Setting fields with decimals
        qrCode.TaxableBasisOfVAT_StandardRate = 1234.5m;
        qrCode.TotalVAT_StandardRate = 23.456m; // Should round to 23.46

        // Expected Output with correctly formatted decimal fields
        var expected = "A:123456789*"
                     + "B:999999990*"
                     + "C:PT*"
                     + "D:FT*"
                     + "E:N*"
                     + "F:20241008*"
                     + "G:FT20241008001*"
                     + "H:0*"
                     + "I1:PT*"
                     + "I7:1234.50*"
                     + "I8:23.46*"
                     + "N:23.34*"
                     + "O:100.34*"
                     + "Q:HASH1234*"
                     + "R:CERT12345*"
                     + "S:Payment via IBAN;ATM Ref. 123456";

        // Actual Output
        var actual = qrCode.GenerateQRCode();

        // Assert using FluentAssertions
        actual.Should().Be(expected);
    }

    [Fact]
    public void GenerateQRCode_ShouldIncludeTaxCountryRegionWithZero_WhenNoVATRate()
    {
        var qrCode = _qrCode;
        // Setup: No VAT rate indicated, TaxCountryRegion should be I1:0
        qrCode.TaxCountryRegion = "0";

        // Expected Output with I1:0
        var expected = "A:123456789*"
                     + "B:999999990*"
                     + "C:PT*"
                     + "D:FT*"
                     + "E:N*"
                     + "F:20241008*"
                     + "G:FT20241008001*"
                     + "H:0*"
                     + "I1:0*"
                     + "N:23.34*"
                     + "O:100.34*"
                     + "Q:HASH1234*"
                     + "R:CERT12345*"
                     + "S:Payment via IBAN;ATM Ref. 123456";

        // Actual Output
        var actual = qrCode.GenerateQRCode();

        // Assert using FluentAssertions
        actual.Should().Be(expected);
    }
}
