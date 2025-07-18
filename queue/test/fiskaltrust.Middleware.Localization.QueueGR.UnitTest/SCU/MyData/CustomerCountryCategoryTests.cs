using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.v2.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest;

public class CustomerCountryCategoryTests
{
    [Fact]
    public void GetCustomerCountryCategory_WithNullCustomer_ReturnsDomestic()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = null
        };

        var category = receiptRequest.GetCustomerCountryCategory();

        category.Should().Be(CustomerCountryCategory.Domestic);
    }

    [Fact]
    public void GetCustomerCountryCategory_WithNullCountryCode_ReturnsDomestic()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCountry = null
            }
        };

        var category = receiptRequest.GetCustomerCountryCategory();

        category.Should().Be(CustomerCountryCategory.Domestic);
    }

    [Theory]
    [InlineData("GR")]
    [InlineData("gr")]
    [InlineData("Gr")]
    [InlineData("EL")]
    [InlineData("el")]
    [InlineData(" GR ")] // With whitespace
    [InlineData("\tEL\n")] // With tab and newline
    public void GetCustomerCountryCategory_WithGreekCountryCode_ReturnsDomestic(string countryCode)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCountry = countryCode
            }
        };

        var category = receiptRequest.GetCustomerCountryCategory();

        category.Should().Be(CustomerCountryCategory.Domestic);
    }

    [Theory]
    // Testing all EU countries except Greece (GR/EL), which is treated as Domestic
    // and tested separately in GetCustomerCountryCategory_WithGreekCountryCode_ReturnsDomestic
    [InlineData("AT")] // Austria
    [InlineData("BE")] // Belgium
    [InlineData("BG")] // Bulgaria
    [InlineData("HR")] // Croatia
    [InlineData("CY")] // Cyprus
    [InlineData("CZ")] // Czech Republic
    [InlineData("DK")] // Denmark
    [InlineData("EE")] // Estonia
    [InlineData("FI")] // Finland
    [InlineData("FR")] // France
    [InlineData("DE")] // Germany
    [InlineData("HU")] // Hungary
    [InlineData("IE")] // Ireland
    [InlineData("IT")] // Italy
    [InlineData("LV")] // Latvia
    [InlineData("LT")] // Lithuania
    [InlineData("LU")] // Luxembourg
    [InlineData("MT")] // Malta
    [InlineData("NL")] // Netherlands
    [InlineData("PL")] // Poland
    [InlineData("PT")] // Portugal
    [InlineData("RO")] // Romania
    [InlineData("SK")] // Slovakia
    [InlineData("SI")] // Slovenia
    [InlineData("ES")] // Spain
    [InlineData("SE")] // Sweden
    // Test case-insensitivity
    [InlineData("at")] // Austria (lowercase)
    [InlineData("Be")] // Belgium (mixed case)
    public void GetCustomerCountryCategory_WithEUCountryCode_ReturnsEU(string countryCode)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCountry = countryCode
            }
        };

        var category = receiptRequest.GetCustomerCountryCategory();

        category.Should().Be(CustomerCountryCategory.EU);
    }

    [Theory]
    // Europe (non-EU)
    [InlineData("GB")] // United Kingdom (non-EU since Brexit)
    [InlineData("CH")] // Switzerland
    [InlineData("NO")] // Norway
    [InlineData("IS")] // Iceland
    [InlineData("UA")] // Ukraine
    [InlineData("RS")] // Serbia
    [InlineData("ME")] // Montenegro
    [InlineData("MD")] // Moldova
    [InlineData("MK")] // North Macedonia
    [InlineData("AL")] // Albania
    [InlineData("BA")] // Bosnia and Herzegovina
    [InlineData("TR")] // Turkey
    // Test case-insensitivity
    [InlineData("gb")] // United Kingdom (lowercase)
    [InlineData("Ch")] // Switzerland (mixed case)
    // Americas
    [InlineData("US")] // United States
    [InlineData("CA")] // Canada
    [InlineData("MX")] // Mexico
    [InlineData("BR")] // Brazil
    [InlineData("AR")] // Argentina
    [InlineData("CL")] // Chile
    // Asia
    [InlineData("CN")] // China
    [InlineData("JP")] // Japan
    [InlineData("IN")] // India
    [InlineData("SG")] // Singapore
    [InlineData("KR")] // South Korea
    [InlineData("AE")] // United Arab Emirates
    [InlineData("IL")] // Israel
    // Africa
    [InlineData("ZA")] // South Africa
    [InlineData("EG")] // Egypt
    [InlineData("MA")] // Morocco
    [InlineData("NG")] // Nigeria
    // Oceania
    [InlineData("AU")] // Australia
    [InlineData("NZ")] // New Zealand
    public void GetCustomerCountryCategory_WithNonEUCountryCode_ReturnsThirdCountry(string countryCode)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCountry = countryCode
            }
        };

        var category = receiptRequest.GetCustomerCountryCategory();

        category.Should().Be(CustomerCountryCategory.ThirdCountry);
    }
}
