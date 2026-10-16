using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using System;
using System.Linq;
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
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

    // === GetCounterPart: the rules verified against the AADE sandbox (fiskaltrust/market-gr#373) ===

    private static ReceiptRequest CreateCounterpartRequest(InvoiceType invoiceType, object? customer, bool fuelInvoice = false, object? counterpartOverride = null)
    {
        var header = new Dictionary<string, object>
        {
            ["invoiceType"] = AADEMappings.InvoiceTypeOverrideMap.Single(x => x.Value == invoiceType).Key
        };
        if (fuelInvoice)
            header["fuelInvoice"] = true;
        var invoice = new Dictionary<string, object> { ["invoiceHeader"] = header };
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
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            ftReceiptCaseData = new { GR = new { mydataoverride = new { invoice } } }
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
    public void GetCounterPart_RetailWithoutFuelInvoice_ReturnsNullAndIgnoresOverride(InvoiceType invoiceType)
    {
        var request = CreateCounterpartRequest(invoiceType, GreekCustomer, counterpartOverride: new { supplyAccountNo = "12345678901" });

        AADEFactory.GetCounterPart(request).Should().BeNull();
    }

    [Theory]
    [InlineData(InvoiceType.Item111, true)]
    [InlineData(InvoiceType.Item113, true)]
    [InlineData(InvoiceType.Item114, true)]
    [InlineData(InvoiceType.Item115, true)]
    [InlineData(InvoiceType.Item112, false)]
    public void GetCounterPart_RetailFuelInvoice_ReturnsCounterpartWhereAadeAllowsIt(InvoiceType invoiceType, bool expected)
    {
        var request = CreateCounterpartRequest(invoiceType, GreekCustomer, fuelInvoice: true);

        var counterpart = AADEFactory.GetCounterPart(request);

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
    public void GetCounterPart_RetailFuelInvoiceWithForeignCustomer_KeepsName()
    {
        var request = CreateCounterpartRequest(InvoiceType.Item111, new MiddlewareCustomer { CustomerVATId = "ATU68541544", CustomerName = "fiskaltrust consulting gmbh", CustomerCountry = "AT" }, fuelInvoice: true);

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart!.country.Should().Be(CountryType.AT);
        counterpart.name.Should().Be("fiskaltrust consulting gmbh");
    }

    [Fact]
    public void GetCounterPart_RetailFuelInvoiceWithCustomerWithoutVat_PassesCustomerOn()
    {
        // The caller only sends cbCustomer for an actual buyer; an anonymous fuel sale has no cbCustomer.
        // A customer without VAT number is passed on and rejected by myDATA.
        var request = CreateCounterpartRequest(InvoiceType.Item111, new MiddlewareCustomer { CustomerName = "Buyer", CustomerCountry = "GR" }, fuelInvoice: true);

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().BeNull();
    }

    [Fact]
    public void GetCounterPart_RetailFuelInvoiceWithoutCustomer_ReturnsNull()
    {
        var request = CreateCounterpartRequest(InvoiceType.Item111, null, fuelInvoice: true);

        AADEFactory.GetCounterPart(request).Should().BeNull();
    }

    [Fact]
    public void GetCounterPart_InvoiceWithCustomerWithoutVat_KeepsCounterpartAsBefore()
    {
        // Unchanged behaviour on non-retail types: the customer is passed on and myDATA decides
        // (e.g. a third-country buyer on 1.3 without VAT number raised no counterpart error in the sandbox).
        var request = CreateCounterpartRequest(InvoiceType.Item11, new MiddlewareCustomer { CustomerName = "Buyer", CustomerCountry = "AT" });

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().BeNull();
    }

    [Fact]
    public void GetCounterPart_OverrideIsMergedOnTopOfCustomer()
    {
        var request = CreateCounterpartRequest(InvoiceType.Item111, GreekCustomer, fuelInvoice: true, counterpartOverride: new { supplyAccountNo = "12345678901", documentIdNo = "AB123" });

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart!.vatNumber.Should().Be("123456789");
        counterpart.supplyAccountNo.Should().Be("12345678901");
        counterpart.documentIdNo.Should().Be("AB123");
    }

    [Fact]
    public void GetCounterPart_OverrideNameForGreekCounterpart_IsPassedThrough()
    {
        // AADE rejects it, but overrides are passed through as-is and AADE stays the authority on their content.
        var request = CreateCounterpartRequest(InvoiceType.Item11, GreekCustomer, counterpartOverride: new { name = "Πελάτης" });

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart!.name.Should().Be("Πελάτης");
    }

    [Fact]
    public void GetCounterPart_OverrideWithoutCustomer_CreatesCounterpartFromOverride()
    {
        var request = CreateCounterpartRequest(InvoiceType.Item11, null, counterpartOverride: new { vatNumber = "987654321", country = "GR" });

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart!.vatNumber.Should().Be("987654321");
        counterpart.country.Should().Be(CountryType.GR);
        counterpart.branch.Should().Be(0);
    }

    [Theory]
    [InlineData(InvoiceType.Item11)]
    [InlineData(InvoiceType.Item111)]
    public void GetCounterPart_OverrideWithoutAnyVatNumber_IsPassedToMyData(InvoiceType invoiceType)
    {
        // The middleware does not validate the content; myDATA rejects a counterpart without vatNumber.
        var request = CreateCounterpartRequest(invoiceType, new MiddlewareCustomer { CustomerName = "Buyer", CustomerCountry = "GR" }, fuelInvoice: true,
            counterpartOverride: new { supplyAccountNo = "12345678901" });

        var counterpart = AADEFactory.GetCounterPart(request);

        counterpart.Should().NotBeNull();
        counterpart!.vatNumber.Should().BeNull();
        counterpart.supplyAccountNo.Should().Be("12345678901");
    }

    [Fact]
    public void GetCounterPart_OverrideWithoutAnyCountry_Throws()
    {
        // PartyType.country cannot be empty and would silently default to AD, so this is the one local check.
        var request = CreateCounterpartRequest(InvoiceType.Item11, new MiddlewareCustomer { CustomerVATId = "EL123456789" }, counterpartOverride: new { supplyAccountNo = "12345678901" });

        var act = () => AADEFactory.GetCounterPart(request);

        act.Should().Throw<ArgumentException>().WithMessage("*country cannot be determined*");
    }
}
