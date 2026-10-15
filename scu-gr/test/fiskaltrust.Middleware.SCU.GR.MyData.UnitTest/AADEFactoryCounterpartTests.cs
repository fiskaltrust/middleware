using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class AADEFactoryCounterpartTests
{
    [Fact]
    public void GetCounterPart_WithoutCustomer_ReturnsNull()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
        };
        
        var counterpart = AADEFactory.GetCounterPart(receiptRequest);

        counterpart.Should().BeNull();
    }
    
    [Fact]
    public void GetCounterPart_WithGreekCustomerAndELPrefix_ReturnsCounterpartWithoutPrefix()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "EL123456789",
                CustomerName = "Greek Company",
                CustomerCountry = "GR"
            }
        };
        
        var counterpart = AADEFactory.GetCounterPart(receiptRequest);
        
        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().Be("123456789");
        counterpart.country.Should().Be(CountryType.GR);
        counterpart.branch.Should().Be(0);
        counterpart.address.Should().BeNull();
        counterpart.name.Should().BeNull();
    }
    
    [Fact]
    public void GetCounterPart_WithGreekCustomerAndGRPrefix_ReturnsCounterpartWithoutPrefix()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "GR987654321",
                CustomerName = "Another Greek Company",
                CustomerCountry = "GR"
            }
        };
        
        var counterpart = AADEFactory.GetCounterPart(receiptRequest);
        
        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().Be("987654321");
        counterpart.country.Should().Be(CountryType.GR);
        counterpart.branch.Should().Be(0);
        counterpart.address.Should().BeNull();
        counterpart.name.Should().BeNull();
    }
    
    [Fact]
    public void GetCounterPart_WithAustrianCustomer_ReturnsCounterpartWithAddressData()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU12345678",
                CustomerName = "Austrian Company GmbH",
                CustomerStreet = "Hauptstrasse 1",
                CustomerCity = "Vienna",
                CustomerZip = "1010",
                CustomerCountry = "AT"
            }
        };
        
        var counterpart = AADEFactory.GetCounterPart(receiptRequest);
        
        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().Be("ATU12345678");
        counterpart.country.Should().Be(CountryType.AT);
        counterpart.branch.Should().Be(0);
        counterpart.address.Should().NotBeNull();
        counterpart.address!.street.Should().Be("Hauptstrasse 1");
        counterpart.address.city.Should().Be("Vienna");
        counterpart.address.postalCode.Should().Be("1010");
        counterpart.name.Should().Be("Austrian Company GmbH");
    }
    
    [Theory]
    [MemberData(nameof(EUCountriesData))]
    public void GetCounterPart_WithEUCountry_ReturnsCorrectCountryCode(string countryCode, CountryType expectedCountryType)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = $"{countryCode}12345678",
                CustomerName = $"{countryCode} Company Ltd",
                CustomerStreet = "Test Street 1",
                CustomerCity = "Test City",
                CustomerZip = "12345",
                CustomerCountry = countryCode
            }
        };
        
        var counterpart = AADEFactory.GetCounterPart(receiptRequest);
        
        counterpart.Should().NotBeNull();
        counterpart!.country.Should().Be(expectedCountryType);
        // Address is set whenever postalCode and city are provided
        counterpart.address.Should().NotBeNull();
        counterpart.address.postalCode.Should().Be("12345");
        counterpart.address.city.Should().Be("Test City");

        if (countryCode == "GR")
        {
            counterpart.name.Should().BeNull("domestic GR customers should not have name set");
        }
        else
        {
            counterpart.name.Should().Be($"{countryCode} Company Ltd");
        }
    }
    
    [Theory]
    [MemberData(nameof(NonEUCountriesData))]
    public void GetCounterPart_WithNonEUCountry_ReturnsCorrectCountryCode(string countryCode, CountryType expectedCountryType)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = $"{countryCode}12345678",
                CustomerName = $"{countryCode} Company Ltd",
                CustomerStreet = "Test Street 1",
                CustomerCity = "Test City",
                CustomerZip = "12345",
                CustomerCountry = countryCode
            }
        };
        
        var counterpart = AADEFactory.GetCounterPart(receiptRequest);
        
        counterpart.Should().NotBeNull();
        counterpart!.country.Should().Be(expectedCountryType);  
        counterpart.address.Should().NotBeNull();
        counterpart.name.Should().Be($"{countryCode} Company Ltd");
    }
    
    public static IEnumerable<object[]> EUCountriesData()
    {
        // List of EU country codes and their corresponding CountryType
        yield return new object[] { "AT", CountryType.AT }; // Austria
        yield return new object[] { "BE", CountryType.BE }; // Belgium
        yield return new object[] { "BG", CountryType.BG }; // Bulgaria
        yield return new object[] { "HR", CountryType.HR }; // Croatia
        yield return new object[] { "CY", CountryType.CY }; // Cyprus
        yield return new object[] { "CZ", CountryType.CZ }; // Czech Republic
        yield return new object[] { "DK", CountryType.DK }; // Denmark
        yield return new object[] { "EE", CountryType.EE }; // Estonia
        yield return new object[] { "FI", CountryType.FI }; // Finland
        yield return new object[] { "FR", CountryType.FR }; // France
        yield return new object[] { "DE", CountryType.DE }; // Germany
        yield return new object[] { "GR", CountryType.GR }; // Greece
        yield return new object[] { "HU", CountryType.HU }; // Hungary
        yield return new object[] { "IE", CountryType.IE }; // Ireland
        yield return new object[] { "IT", CountryType.IT }; // Italy
        yield return new object[] { "LV", CountryType.LV }; // Latvia
        yield return new object[] { "LT", CountryType.LT }; // Lithuania
        yield return new object[] { "LU", CountryType.LU }; // Luxembourg
        yield return new object[] { "MT", CountryType.MT }; // Malta
        yield return new object[] { "NL", CountryType.NL }; // Netherlands
        yield return new object[] { "PL", CountryType.PL }; // Poland
        yield return new object[] { "PT", CountryType.PT }; // Portugal
        yield return new object[] { "RO", CountryType.RO }; // Romania
        yield return new object[] { "SK", CountryType.SK }; // Slovakia
        yield return new object[] { "SI", CountryType.SI }; // Slovenia
        yield return new object[] { "ES", CountryType.ES }; // Spain
        yield return new object[] { "SE", CountryType.SE }; // Sweden
    }
    
    public static IEnumerable<object[]> NonEUCountriesData()
    {
        // Europe (non-EU)
        yield return new object[] { "CH", CountryType.CH }; // Switzerland
        yield return new object[] { "GB", CountryType.GB }; // United Kingdom
        yield return new object[] { "NO", CountryType.NO }; // Norway
        yield return new object[] { "IS", CountryType.IS }; // Iceland
        yield return new object[] { "UA", CountryType.UA }; // Ukraine
        yield return new object[] { "RS", CountryType.RS }; // Serbia
        yield return new object[] { "ME", CountryType.ME }; // Montenegro
        yield return new object[] { "MD", CountryType.MD }; // Moldova
        yield return new object[] { "MK", CountryType.MK }; // North Macedonia
        yield return new object[] { "AL", CountryType.AL }; // Albania
        yield return new object[] { "BA", CountryType.BA }; // Bosnia and Herzegovina
        yield return new object[] { "TR", CountryType.TR }; // Turkey
        
        // Americas
        yield return new object[] { "US", CountryType.US }; // United States
        yield return new object[] { "CA", CountryType.CA }; // Canada
        yield return new object[] { "MX", CountryType.MX }; // Mexico
        yield return new object[] { "BR", CountryType.BR }; // Brazil
        yield return new object[] { "AR", CountryType.AR }; // Argentina
        yield return new object[] { "CL", CountryType.CL }; // Chile
        
        // Asia
        yield return new object[] { "CN", CountryType.CN }; // China
        yield return new object[] { "JP", CountryType.JP }; // Japan
        yield return new object[] { "IN", CountryType.IN }; // India
        yield return new object[] { "SG", CountryType.SG }; // Singapore
        yield return new object[] { "KR", CountryType.KR }; // South Korea
        yield return new object[] { "AE", CountryType.AE }; // United Arab Emirates
        yield return new object[] { "IL", CountryType.IL }; // Israel
        
        // Africa
        yield return new object[] { "ZA", CountryType.ZA }; // South Africa
        yield return new object[] { "EG", CountryType.EG }; // Egypt
        yield return new object[] { "MA", CountryType.MA }; // Morocco
        yield return new object[] { "NG", CountryType.NG }; // Nigeria
        
        // Oceania
        yield return new object[] { "AU", CountryType.AU }; // Australia
        yield return new object[] { "NZ", CountryType.NZ }; // New Zealand
    }

    // === CreateCounterpart: the rules verified against the AADE sandbox (fiskaltrust/market-gr#373) ===

    private static ReceiptRequest CreateCounterpartRequest(object? customer, bool fuelInvoice = false, object? counterpartOverride = null, bool b2b = false)
    {
        var invoice = new Dictionary<string, object>();
        if (fuelInvoice)
            invoice["invoiceHeader"] = new { fuelInvoice = true };
        if (counterpartOverride != null)
            invoice["counterpart"] = counterpartOverride;
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            cbCustomer = customer,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(b2b ? ReceiptCase.InvoiceB2B0x1002 : ReceiptCase.PointOfSaleReceipt0x0001),
            ftReceiptCaseData = invoice.Count > 0 ? new { GR = new { mydataoverride = new { invoice } } } : null
        };
    }

    private static readonly object GreekCustomer = new MiddlewareCustomer
    {
        CustomerVATId = "EL123456789",
        CustomerName = "Πελάτης",
        CustomerCountry = "GR",
        CustomerZip = "12345",
        CustomerCity = "Αθήνα"
    };

    [Theory]
    [InlineData(InvoiceType.Item111)]
    [InlineData(InvoiceType.Item112)]
    [InlineData(InvoiceType.Item114)]
    public void CreateCounterpart_RetailWithoutFuelInvoice_ReturnsNullAndIgnoresOverride(InvoiceType invoiceType)
    {
        var request = CreateCounterpartRequest(GreekCustomer, counterpartOverride: new { supplyAccountNo = "12345678901" });

        AADEFactory.CreateCounterpart(request, invoiceType).Should().BeNull();
    }

    [Theory]
    [InlineData(InvoiceType.Item111, true)]
    [InlineData(InvoiceType.Item113, true)]
    [InlineData(InvoiceType.Item114, true)]
    [InlineData(InvoiceType.Item115, true)]
    [InlineData(InvoiceType.Item112, false)]
    public void CreateCounterpart_RetailFuelInvoice_ReturnsCounterpartWhereAadeAllowsIt(InvoiceType invoiceType, bool expected)
    {
        var request = CreateCounterpartRequest(GreekCustomer, fuelInvoice: true);

        var counterpart = AADEFactory.CreateCounterpart(request, invoiceType);

        if (expected)
        {
            counterpart.Should().NotBeNull();
            counterpart!.vatNumber.Should().Be("123456789");
            counterpart.name.Should().BeNull();
            counterpart.address.postalCode.Should().Be("12345");
        }
        else
        {
            counterpart.Should().BeNull();
        }
    }

    [Fact]
    public void CreateCounterpart_RetailFuelInvoiceWithForeignCustomer_KeepsName()
    {
        var request = CreateCounterpartRequest(new MiddlewareCustomer { CustomerVATId = "ATU68541544", CustomerName = "fiskaltrust consulting gmbh", CustomerCountry = "AT" }, fuelInvoice: true);

        var counterpart = AADEFactory.CreateCounterpart(request, InvoiceType.Item111);

        counterpart!.country.Should().Be(CountryType.AT);
        counterpart.name.Should().Be("fiskaltrust consulting gmbh");
    }

    [Fact]
    public void CreateCounterpart_AnonymousRetailFuelSale_ReturnsNull()
    {
        var request = CreateCounterpartRequest(new MiddlewareCustomer { CustomerName = "Buyer", CustomerCountry = "GR" }, fuelInvoice: true);

        AADEFactory.CreateCounterpart(request, InvoiceType.Item111).Should().BeNull();
    }

    [Fact]
    public void CreateCounterpart_InvoiceWithCustomerWithoutVat_KeepsCounterpartAsBefore()
    {
        var request = CreateCounterpartRequest(new MiddlewareCustomer { CustomerName = "Buyer", CustomerCountry = "AT" }, b2b: true);

        var counterpart = AADEFactory.CreateCounterpart(request, InvoiceType.Item11);

        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().BeNull();
    }

    [Fact]
    public void CreateCounterpart_OverrideIsMergedOnTopOfCustomer()
    {
        var request = CreateCounterpartRequest(GreekCustomer, fuelInvoice: true, counterpartOverride: new { supplyAccountNo = "12345678901", documentIdNo = "AB123" });

        var counterpart = AADEFactory.CreateCounterpart(request, InvoiceType.Item111);

        counterpart!.vatNumber.Should().Be("123456789");
        counterpart.supplyAccountNo.Should().Be("12345678901");
        counterpart.documentIdNo.Should().Be("AB123");
    }

    [Fact]
    public void CreateCounterpart_OverrideNameForGreekCounterpart_IsPassedThrough()
    {
        // AADE rejects it, but overrides are passed through as-is and AADE stays the authority on their content.
        var request = CreateCounterpartRequest(GreekCustomer, counterpartOverride: new { name = "Πελάτης" }, b2b: true);

        var counterpart = AADEFactory.CreateCounterpart(request, InvoiceType.Item11);

        counterpart!.name.Should().Be("Πελάτης");
    }

    [Fact]
    public void CreateCounterpart_OverrideWithoutCustomer_CreatesCounterpartFromOverride()
    {
        var request = CreateCounterpartRequest(null, counterpartOverride: new { vatNumber = "987654321", country = "GR" }, b2b: true);

        var counterpart = AADEFactory.CreateCounterpart(request, InvoiceType.Item11);

        counterpart!.vatNumber.Should().Be("987654321");
        counterpart.country.Should().Be(CountryType.GR);
        counterpart.branch.Should().Be(0);
    }

    [Theory]
    [InlineData(InvoiceType.Item11)]
    [InlineData(InvoiceType.Item111)]
    public void CreateCounterpart_OverrideWithoutAnyVatNumber_Throws(InvoiceType invoiceType)
    {
        var request = CreateCounterpartRequest(new MiddlewareCustomer { CustomerName = "Buyer", CustomerCountry = "GR" }, fuelInvoice: true,
            counterpartOverride: new { supplyAccountNo = "12345678901" }, b2b: invoiceType == InvoiceType.Item11);

        var act = () => AADEFactory.CreateCounterpart(request, invoiceType);

        act.Should().Throw<ArgumentException>().WithMessage("*requires the buyer's VAT number*");
    }

    [Fact]
    public void CreateCounterpart_OverrideWithoutCustomerAndCountry_Throws()
    {
        var request = CreateCounterpartRequest(null, counterpartOverride: new { vatNumber = "987654321" }, b2b: true);

        var act = () => AADEFactory.CreateCounterpart(request, InvoiceType.Item11);

        act.Should().Throw<ArgumentException>().WithMessage("*counterpart.country*");
    }
}
