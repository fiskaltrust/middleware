using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Exports.SAFTPT;

public class SAFTMappingCustomerTests
{
    private readonly SaftExporter _saftExporter;

    public SAFTMappingCustomerTests()
    {
        _saftExporter = new SaftExporter();
    }

    [Theory]
    [InlineData("123456789", true)]    // Valid NIF - calculated correctly
    [InlineData("200000004", true)]    // Valid NIF - company starting with 2
    [InlineData("100000002", true)]    // Valid NIF - starting with 1
    [InlineData("500000000", true)]    // Valid NIF - public entity starting with 5
    [InlineData("600000001", true)]    // Valid NIF - public administration starting with 6
    [InlineData("700000003", true)]    // Valid NIF - starting with 7
    [InlineData("800000005", true)]    // Valid NIF - starting with 8
    [InlineData("900000007", true)]    // Valid NIF - starting with 9
    public void IsValidPortugueseTaxId_ValidNIFs_ShouldReturnTrue(string taxId, bool expected)
    {
        // Act
        var result = SaftExporter.IsValidPortugueseTaxId(taxId);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", false)]            // Empty string
    [InlineData("   ", false)]         // Whitespace only
    [InlineData(null, false)]          // Null
    [InlineData("12345678", false)]    // Too short
    [InlineData("1234567890", false)]  // Too long
    [InlineData("12345678a", false)]   // Contains letters
    [InlineData("123-456-789", false)] // Contains hyphens
    [InlineData("023456789", false)]   // Invalid first digit (0)
    [InlineData("423456789", false)]   // Invalid first digit (4)
    [InlineData("123456788", false)]   // Invalid check digit
    [InlineData("245567891", false)]   // Invalid check digit
    public void IsValidPortugueseTaxId_InvalidNIFs_ShouldReturnFalse(string taxId, bool expected)
    {
        // Act
        var result = SaftExporter.IsValidPortugueseTaxId(taxId);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetCustomerData_WithValidPortugueseTaxId_ShouldUseProvidedCountryNotPT()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST001",
                CustomerName = "João Silva",
                CustomerVATId = "123456789", // Valid Portuguese NIF
                CustomerStreet = "Rua das Flores, 123",
                CustomerCity = "Lisboa",
                CustomerZip = "1000-001",
                CustomerCountry = "Spain" // This should NOT be overridden to PT - CustomerCountry has priority
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST001");
        customer.CompanyName.Should().Be("João Silva");
        customer.CustomerTaxID.Should().Be("123456789");
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("Spain"); // Should keep provided country, not PT
        customer.BillingAddress.AddressDetail.Should().Be("Rua das Flores, 123");
        customer.BillingAddress.City.Should().Be("Lisboa");
        customer.BillingAddress.PostalCode.Should().Be("1000-001");
    }

    [Fact]
    public void GetCustomerData_WithInvalidTaxId_ShouldUseProvidedCountry()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST002",
                CustomerName = "Maria Santos",
                CustomerVATId = "invalid-tax-id",
                CustomerStreet = "Calle Mayor, 456",
                CustomerCity = "Madrid",
                CustomerZip = "28001",
                CustomerCountry = "Spain"
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST002");
        customer.CompanyName.Should().Be("Maria Santos");
        customer.CustomerTaxID.Should().Be("invalid-tax-id");
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("Spain"); // Should keep the provided country
    }

    [Fact]
    public void GetCustomerData_WithValidPortugueseTaxIdAndNoCountry_ShouldSetCountryToPT()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST003",
                CustomerName = "Ana Costa",
                CustomerVATId = "200000004", // Valid Portuguese NIF
                CustomerStreet = "Avenida da República, 789",
                CustomerCity = "Porto",
                CustomerZip = "4000-001",
                CustomerCountry = null // No country provided - should default to PT due to valid NIF
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST003");
        customer.CompanyName.Should().Be("Ana Costa");
        customer.CustomerTaxID.Should().Be("200000004");
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("PT"); // Should be PT due to valid Portuguese NIF and no country provided
    }

    [Fact]
    public void GetCustomerData_WithValidPortugueseTaxIdAndEmptyCountry_ShouldSetCountryToPT()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST_EMPTY",
                CustomerName = "Luis Santos",
                CustomerVATId = "100000002", // Valid Portuguese NIF
                CustomerStreet = "Rua do Comércio, 456",
                CustomerCity = "Braga",
                CustomerZip = "4700-001",
                CustomerCountry = "" // Empty string country - should default to PT due to valid NIF
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST_EMPTY");
        customer.CompanyName.Should().Be("Luis Santos");
        customer.CustomerTaxID.Should().Be("100000002");
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("PT"); // Should be PT due to valid Portuguese NIF and empty country
    }

    [Fact]
    public void GetCustomerData_WithOnlyValidPortugueseTaxId_ShouldSetCountryToPTAndUseDefaults()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = null, // No customer ID
                CustomerName = null, // No customer name
                CustomerVATId = "500000000", // Only valid Portuguese NIF provided
                CustomerStreet = null, // No street
                CustomerCity = null, // No city
                CustomerZip = null, // No zip
                CustomerCountry = null // No country provided
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().NotBeNullOrEmpty(); // Should be generated from hash of VATId
        customer.CompanyName.Should().Be("Desconhecido"); // Default for null customer name
        customer.CustomerTaxID.Should().Be("500000000"); // Should keep the provided valid NIF
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("PT"); // Should be PT due to valid Portuguese NIF
        customer.BillingAddress.AddressDetail.Should().Be("Desconhecido"); // Default for null street
        customer.BillingAddress.City.Should().Be("Desconhecido"); // Default for null city
        customer.BillingAddress.PostalCode.Should().Be("Desconhecido"); // Default for null zip
    }

    [Fact]
    public void GetCustomerData_WithNoTaxIdAndNoCountry_ShouldUseDefaultCountry()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST004",
                CustomerName = "Pedro Oliveira",
                CustomerVATId = null, // No tax ID
                CustomerStreet = "Rua Nova, 321",
                CustomerCity = "Braga",
                CustomerZip = "4700-001",
                CustomerCountry = null // No country provided
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST004");
        customer.CompanyName.Should().Be("Pedro Oliveira");
        customer.CustomerTaxID.Should().Be("999999990"); // Default tax ID
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("Desconhecido"); // Default country
    }

    [Fact]
    public void GetCustomerData_WithEmptyTaxIdAndEmptyCountry_ShouldUseDefaults()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST005",
                CustomerName = "Carlos Ferreira",
                CustomerVATId = "", // Empty tax ID
                CustomerStreet = "Praça Central, 654",
                CustomerCity = "Coimbra",
                CustomerZip = "3000-001",
                CustomerCountry = "" // Empty country
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST005");
        customer.CompanyName.Should().Be("Carlos Ferreira");
        customer.CustomerTaxID.Should().Be("999999990"); // Default tax ID
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("Desconhecido"); // Default country
    }

    [Fact]
    public void GetCustomerData_WithWhitespaceOnlyTaxId_ShouldUseProvidedCountry()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST006",
                CustomerName = "Sofia Martins",
                CustomerVATId = "   ", // Whitespace only
                CustomerStreet = "Rua da Paz, 987",
                CustomerCity = "Faro",
                CustomerZip = "8000-001",
                CustomerCountry = "Portugal"
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("CUST006");
        customer.CompanyName.Should().Be("Sofia Martins");
        customer.CustomerTaxID.Should().Be("   "); // Whitespace is preserved, not converted to default
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("Portugal"); // Should keep provided country
    }

    [Fact]
    public void GetCustomerData_WithNullCustomer_ShouldReturnAnonymousCustomer()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = null
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.CustomerID.Should().Be("0");
        customer.AccountID.Should().Be("Desconhecido");
        customer.CompanyName.Should().Be("Consumidor final");
        customer.CustomerTaxID.Should().Be("999999990");
        customer.BillingAddress.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be("Desconhecido");
        customer.BillingAddress.AddressDetail.Should().Be("Desconhecido");
        customer.BillingAddress.City.Should().Be("Desconhecido");
        customer.BillingAddress.PostalCode.Should().Be("Desconhecido");
    }

    [Theory]
    [InlineData("123456789", "PT", "PT")]      // Valid NIF with PT country should result in PT
    [InlineData("200000004", "Spain", "Spain")] // Valid NIF with different country should keep original country
    [InlineData("123456789", null, "PT")]       // Valid NIF with no country should result in PT
    [InlineData("invalid", "Germany", "Germany")] // Invalid NIF should keep original country
    [InlineData("123456788", "France", "France")] // Invalid check digit should keep original country
    [InlineData("", "Italy", "Italy")]           // Empty NIF should keep original country
    [InlineData(null, "Spain", "Spain")]         // Null NIF should keep original country
    [InlineData("123456789", "", "PT")]          // Valid NIF with empty country should result in PT
    [InlineData("invalid", null, "Desconhecido")] // Invalid NIF with no country should default
    public void GetCustomerData_WithVariousTaxIdsAndCountries_ShouldSetCountryCorrectly(string taxId, string providedCountry, string expectedCountry)
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            cbCustomer = new MiddlewareCustomer
            {
                CustomerId = "CUST_VAR",
                CustomerName = "Test Customer",
                CustomerVATId = taxId,
                CustomerCountry = providedCountry
            }
        };

        // Act
        var customer = _saftExporter.GetCustomerData(receiptRequest);

        // Assert
        customer.Should().NotBeNull();
        customer.BillingAddress.Country.Should().Be(expectedCountry);
    }
}