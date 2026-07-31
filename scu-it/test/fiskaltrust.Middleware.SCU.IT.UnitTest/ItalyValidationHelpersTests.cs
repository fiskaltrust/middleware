using FluentAssertions;
using fiskaltrust.Middleware.SCU.IT.Abstraction.Validation;
using Xunit;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class ItalyValidationHelpersTests
    {
        [Theory]
        // Real partite IVA; the check digit of each was verified by hand.
        [InlineData("01606720215")]
        [InlineData("01114601006")]
        [InlineData("00905811006")]
        [InlineData("00484960588")]
        // The IT country prefix is accepted, as are surrounding whitespace and lower case.
        [InlineData("IT01606720215")]
        [InlineData("it01606720215")]
        [InlineData("  IT01606720215  ")]
        public void IsValidPartitaIva_WithValidValue_ReturnsTrue(string partitaIva)
        {
            ItalyValidationHelpers.IsValidPartitaIva(partitaIva).Should().BeTrue();
        }

        [Theory]
        [InlineData("01606720216")]         // wrong check digit
        [InlineData("0160672021")]          // 10 digits
        [InlineData("016067202150")]        // 12 digits
        [InlineData("0160672021A")]         // right length, not all digits
        [InlineData("DE123456789")]         // 11 characters, but a foreign prefix is not tolerated
        [InlineData("IT")]                  // prefix only
        [InlineData("IT0160672021")]        // only 10 digits once the prefix is stripped
        [InlineData("00000000000")]         // satisfies the checksum, but is a placeholder
        [InlineData("016.067.202.15")]      // internal separators are not stripped
        [InlineData("RSSMRA80A01H501U")]    // a codice fiscale is not a partita IVA
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidPartitaIva_WithInvalidValue_ReturnsFalse(string partitaIva)
        {
            ItalyValidationHelpers.IsValidPartitaIva(partitaIva).Should().BeFalse();
        }

        [Theory]
        [InlineData("MRTMTT25D09F205Z")]        // sum 155 -> 25 -> 'Z'
        [InlineData("RSSMRA80A01H501U")]        // sum 98 -> 20 -> 'U'
        [InlineData("BNCNNA90A41F205W")]        // female, day 41; sum 100 -> 22 -> 'W'
        [InlineData("  rssmra80a01h501u  ")]    // trimmed and upper-cased
        // Omocodia: digits replaced by their substitute letters, with the CIN recalculated on the
        // substituted code.
        [InlineData("MRTMTT25D09F20RU")]
        [InlineData("RSSMRA80A01H5LMX")]
        // Legal entities use their partita IVA as codice fiscale.
        [InlineData("01606720215")]
        [InlineData("IT01606720215")]
        public void IsValidCodiceFiscale_WithValidValue_ReturnsTrue(string codiceFiscale)
        {
            ItalyValidationHelpers.IsValidCodiceFiscale(codiceFiscale).Should().BeTrue();
        }

        [Theory]
        [InlineData("MRTMTT25D09F205A")]        // wrong check character
        [InlineData("RSSMRA80A01H501")]         // 15 characters
        [InlineData("RSSMRA80A01H501UU")]       // 17 characters
        [InlineData("RSS1RA80A01H501U")]        // digit inside the surname/name block
        [InlineData("RSSMRA8OA01H501U")]        // 'O' is not an omocodia letter
        [InlineData("RSSMRA80Z01H501U")]        // 'Z' is not a month letter
        [InlineData("RSSMRA80A81H501U")]        // day 81 is out of range
        [InlineData("RSSMRA80A00H501U")]        // day 00 is out of range
        [InlineData("DE123456789")]
        [InlineData("01606720216")]             // 11 digits, wrong check digit
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidCodiceFiscale_WithInvalidValue_ReturnsFalse(string codiceFiscale)
        {
            ItalyValidationHelpers.IsValidCodiceFiscale(codiceFiscale).Should().BeFalse();
        }

        /// <summary>
        /// The check character of an omocode is recalculated on the substituted code, so the CIN must be
        /// computed over the characters exactly as they are written. An implementation that decodes the
        /// omocodia letter 'R' back to '5' before computing the CIN would wrongly accept this value,
        /// because 'Z' is the check character of the base code MRTMTT25D09F205Z.
        /// </summary>
        [Fact]
        public void IsValidCodiceFiscale_WithOmocodiaAndTheBaseCodeCheckCharacter_ReturnsFalse()
        {
            ItalyValidationHelpers.IsValidCodiceFiscale("MRTMTT25D09F20RZ").Should().BeFalse();
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("  it01606720215 ", "IT01606720215")]
        [InlineData("RSSMRA80A01H501U", "RSSMRA80A01H501U")]
        public void Normalize_TrimsAndUpperCases(string value, string expected)
        {
            ItalyValidationHelpers.Normalize(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("  it01606720215 ", "01606720215")]
        [InlineData("IT", "")]
        [InlineData("12345", "12345")]                       // invalid input is not blanked out
        [InlineData("IT12345", "12345")]                     // the prefix is stripped unconditionally
        [InlineData("RSSMRA80A01H501U", "RSSMRA80A01H501U")]
        public void NormalizeVatId_StripsTheCountryPrefixUnconditionally(string value, string expected)
        {
            ItalyValidationHelpers.NormalizeVatId(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("  it01606720215 ", "01606720215")]           // an IT prefixed partita IVA is stripped
        [InlineData("IT12345678901", "12345678901")]              // 11 digits remain, so it is a partita IVA
        [InlineData("RSSMRA80A01H501U", "RSSMRA80A01H501U")]
        [InlineData("ITLMRA80A01H501U", "ITLMRA80A01H501U")]      // a surname starting with IT is left alone
        [InlineData("IT12345", "IT12345")]                        // not 11 digits, so nothing is stripped
        public void NormalizeTaxCode_StripsTheCountryPrefixOnlyForAPartitaIva(string value, string expected)
        {
            ItalyValidationHelpers.NormalizeTaxCode(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, null, "")]
        [InlineData("RSSMRA80A01H501U", null, "RSSMRA80A01H501U")]
        [InlineData(null, "01606720215", "01606720215")]
        [InlineData(null, "IT01606720215", "01606720215")]
        [InlineData("RSSMRA80A01H501U", "01606720215", "RSSMRA80A01H501U")]   // the codice fiscale wins
        [InlineData("", "01606720215", "01606720215")]                        // an empty CustomerId falls through
        [InlineData("   ", "01606720215", "01606720215")]                     // ... and so does a blank one
        [InlineData("IT01606720215", null, "01606720215")]                    // CustomerId carrying a partita IVA
        [InlineData("ITLMRA80A01H501U", null, "ITLMRA80A01H501U")]            // a surname starting with IT survives
        public void SelectCustomerTaxId_PrefersTheCodiceFiscaleAndNormalizesBoth(string customerId, string customerVatId, string expected)
        {
            ItalyValidationHelpers.SelectCustomerTaxId(customerId, customerVatId).Should().Be(expected);
        }
    }
}
