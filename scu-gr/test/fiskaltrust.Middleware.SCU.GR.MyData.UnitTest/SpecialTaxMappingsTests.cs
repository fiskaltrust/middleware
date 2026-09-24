using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.SCU.MyData
{
    public class SpecialTaxMappingsTests
    {
        [Fact]
        public void IsSpecialTaxItem_ShouldReturnTrue_WhenTypeOfServiceIsF()
        {
            var chargeItem = new ChargeItem
            {
                ftChargeItemCase = (ChargeItemCase) 0x47520000000000F0 // TypeOfService = 0xF
            };

            var result = SpecialTaxMappings.IsSpecialTaxItem(chargeItem);
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSpecialTaxItem_ShouldReturnFalse_WhenTypeOfServiceIsNot0xF()
        {
            var chargeItem = new ChargeItem
            {
                ftChargeItemCase = (ChargeItemCase) 0x4752000000000001 // TypeOfService = 0x1
            };

            var result = SpecialTaxMappings.IsSpecialTaxItem(chargeItem);
            result.Should().BeFalse();
        }

        [Fact]
        public void GetWithholdingTaxMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid description";

            var mapping = SpecialTaxMappings.GetWithholdingTaxMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("Περιπτ. β'- Τόκοι - 15%", 1, 15, false)]
        [InlineData("Περιπτ. γ' - Δικαιώματα - 20%", 2, 20, false)]
        [InlineData("Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%", 3, 20, false)]
        [InlineData("Περιπτ. δ' - Τεχνικά Έργα - 3%", 4, 3, false)]
        [InlineData("Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%", 5, 1, false)]
        [InlineData("Λοιπά Αγαθά 4%", 6, 4, false)]
        [InlineData("Παροχή Υπηρεσιών 8%", 7, 8, false)]
        [InlineData("Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%", 8, 4, false)]
        [InlineData("Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%", 9, 10, false)]
        [InlineData("Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%", 10, 15, false)]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013", 11, null, true)]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού", 12, 15, false)]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού", 13, 10, false)]
        [InlineData("Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", 14, null, true)]
        [InlineData("Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013", 15, null, true)]
        [InlineData("Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)", 16, null, true)]
        [InlineData("Λοιπές Παρακρατήσεις Φόρου", 17, null, true)]
        [InlineData("Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013", 18, 5, false)]
        public void GetWithholdingTaxMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, int? expectedPercentage, bool expectedIsFixed)
        {
            // Act
            var mapping = SpecialTaxMappings.GetWithholdingTaxMapping(description);

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }

        [Fact]
        public void GetFeeMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid fee description";

            var mapping = SpecialTaxMappings.GetFeeMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%", 1, 12, false)]
        [InlineData("Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%", 2, 15, false)]
        [InlineData("Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%", 3, 18, false)]
        [InlineData("Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%", 4, 20, false)]
        [InlineData("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)", 5, 12, false)]
        [InlineData("Τέλος στη συνδρομητική τηλεόραση 10%", 6, 10, false)]
        [InlineData("Τέλος συνδρομητών σταθερής τηλεφωνίας 5%", 7, 5, false)]
        [InlineData("Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", 8, null, true)]
        [InlineData("Εισφορά δακοκτονίας 2%", 9, 2, false)]
        [InlineData("Λοιπά τέλη", 10, null, true)]
        [InlineData("Τέλη Λοιπών Φόρων", 11, null, true)]
        [InlineData("Εισφορά δακοκτονίας", 12, null, true)]
        [InlineData("Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)", 13, 10, false)]
        [InlineData("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)", 14, 10, false)]
        [InlineData("Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)", 15, 0, false)]
        [InlineData("Εισφορά προστασίας περιβάλλοντος πλαστικών προϊόντων 0,04 λεπτά ανά τεμάχιο [άρθρο 4 ν. 4736/2020]", 16, null, true)]
        [InlineData("Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο [άρθρο 80 ν. 4819/2021]", 17, null, true)]
        [InlineData("Τέλος διαμονής παρεπιδημούντων", 18, null, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων", 19, null, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης", 20, null, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των καζίνο", 21, null, true)]
        [InlineData("Λοιπά τέλη επί των ακαθάριστων εσόδων", 22, null, true)]
        public void GetFeeMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, int? expectedPercentage, bool expectedIsFixed)
        {
            // Act
            var mapping = SpecialTaxMappings.GetFeeMapping(description);

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }

        [Fact]
        public void GetStampDutyMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid stamp duty description";

            var mapping = SpecialTaxMappings.GetStampDutyMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("Συντελεστής 1,2 %", 1, "1,2", false)]
        [InlineData("Συντελεστής 2,4 %", 2, "2,4", false)]
        [InlineData("Συντελεστής 3,6 %", 3, "3,6", false)]
        [InlineData("Λοιπές περιπτώσεις Χαρτοσήμου", 4, null, true)]
        public void GetStampDutyMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, string? expectedPercentageStr, bool expectedIsFixed)
        {
            // Act
            var mapping = SpecialTaxMappings.GetStampDutyMapping(description);
            decimal? expectedPercentage = expectedPercentageStr != null ? decimal.Parse(expectedPercentageStr) : null;

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
        }

        [Fact]
        public void GetOtherTaxMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid other tax description";

            var mapping = SpecialTaxMappings.GetOtherTaxMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("α1) ασφάλιστρα κλάδου πυρός 20%", 1, 15, false)]
        [InlineData("α2) ασφάλιστρα κλάδου πυρός 20%", 2, 5, false)]
        [InlineData("β) ασφάλιστρα κλάδου ζωής 4%", 3, 4, false)]
        [InlineData("γ) ασφάλιστρα λοιπών κλάδων 15%.", 4, 15, false)]
        [InlineData("δ) απαλλασσόμενα φόρου ασφαλίστρων 0%.", 5, 0, false)]
        [InlineData("Ξενοδοχεία 1-2 αστέρων 0,50 €", 6, null, true)]
        [InlineData("Ξενοδοχεία 3 αστέρων 1,50 €", 7, null, true)]
        [InlineData("Ξενοδοχεία 4 αστέρων 3,00 €", 8, null, true)]
        [InlineData("Ξενοδοχεία 4 αστέρων 4,00 €", 9, null, true)] // official AADE wording (Appendix 8.5), despite the apparent "4 αστέρων" typo
        [InlineData("Ξενοδοχεία 5 αστέρων 4,00 €", 9, null, true)] // alias: the logically correct 5-star wording
        [InlineData("Ενοικιαζόμενα - επιπλωμένα δωμάτια - διαμερίσματα 0,50 €", 10, null, true)]
        [InlineData("Ειδικός Φόρος στις διαφημίσεις που προβάλλονται από την τηλεόραση (ΕΦΤΔ) 5%", 11, 5, false)]
        [InlineData("3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτούμενα και εισαγόμενα από τρίτες χώρες 10%", 12, 10, false)]
        [InlineData("3.2 Φόρος πολυτελείας 10% επί της τιμής πώλησης προ Φ.Π.Α. για τα εγχωρίως παραγόμενα είδη 10%", 13, 10, false)]
        [InlineData("Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)", 14, 80, false)]
        [InlineData("ασφάλιστρα κλάδου πυρός 20%", 15, 20, false)]
        [InlineData("Λοιποί Τελωνειακοί Δασμοί-Φόροι", 16, null, true)]
        [InlineData("Λοιποί Φόροι", 17, null, true)]
        [InlineData("Επιβαρύνσεις Λοιπών Φόρων", 18, null, true)]
        [InlineData("ΕΦΚ", 19, null, true)]
        [InlineData("Ξενοδοχεία 1-2 αστέρων 1,50€ (ανά Δωμ./Διαμ.)", 20, null, true)]
        [InlineData("Ξενοδοχεία 3 αστέρων 3,00€ (ανά Δωμ./Διαμ.)", 21, null, true)]
        [InlineData("Ξενοδοχεία 4 αστέρων 7,00€ (ανά Δωμ./Διαμ.)", 22, null, true)]
        [InlineData("Ξενοδοχεία 5 αστέρων 10,00€ (ανά Δωμ./Διαμ.)", 23, null, true)]
        [InlineData("Ενοικιαζόμενα επιπλωμένα δωμάτια – διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.)", 24, null, true)]
        [InlineData("Ακίνητα βραχυχρόνιας μίσθωσης 1,50€", 25, null, true)]
        [InlineData("Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 10,00€", 26, null, true)]
        [InlineData("Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€", 27, null, true)]
        [InlineData("Ακίνητα βραχυχρόνιας μίσθωσης 0,50€", 28, null, true)]
        [InlineData("Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 4,00€", 29, null, true)]
        [InlineData("Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 4,00€", 30, null, true)]
        public void GetOtherTaxMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, int? expectedPercentage, bool expectedIsFixed)
        {
            // Act
            var mapping = SpecialTaxMappings.GetOtherTaxMapping(description);

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }

        #region IsVatableSpecialTaxItemTests
        [Theory]
        [InlineData("Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%", 1, true)]
        [InlineData("Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%", 2, true)]
        [InlineData("Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%", 3, true)]
        [InlineData("Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%", 4, true)]
        [InlineData("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)", 5, true)]
        [InlineData("Τέλος στη συνδρομητική τηλεόραση 10%", 6, true)]
        [InlineData("Τέλος συνδρομητών σταθερής τηλεφωνίας 5%", 7, true)]
        [InlineData("Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", 8, true)]
        [InlineData("Εισφορά δακοκτονίας 2%", 9, false)]
        [InlineData("Λοιπά τέλη", 10, true)]
        [InlineData("Τέλη Λοιπών Φόρων", 11, true)]
        [InlineData("Εισφορά δακοκτονίας", 12, false)]
        [InlineData("Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)", 13, true)]
        [InlineData("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)", 14, true)]
        [InlineData("Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)", 15, true)]
        [InlineData("Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο [άρθρο 80 ν. 4819/2021]", 17, true)]
        [InlineData("Τέλος διαμονής παρεπιδημούντων", 18, false)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων", 19, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης", 20, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των καζίνο", 21, false)]
        [InlineData("Λοιπά τέλη επί των ακαθάριστων εσόδων", 22, true)]
        [InlineData("Invalid description", 23, false)]
        [InlineData("INVALID FEE DESC", -1, false)] // unmapped
        [InlineData("", 0, false)] // empty description = no mapping
        public void GetFeeMapping_ShouldHandleAllDefinedMappingsForIsVatableSpecialFee(string description, int expectedCode, bool expectedAcceptsVAT)
        {
            // Arrange
            var chargeItem = new ChargeItem { Description = description };

            // Act
            var result = SpecialTaxMappings.IsVatableSpecialFee(chargeItem);

            // Assert
            result.Should().Be(expectedAcceptsVAT);
        }

        [Theory]
        /* Not special tax item: should always return false for any VAT code and description */
        [InlineData(ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.NormalVatRate, "Λοιπά τέλη", false)]
        [InlineData(ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.DiscountedVatRate1, "Λοιπά τέλη", false)]
        [InlineData(ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.SuperReducedVatRate1, "Λοιπά τέλη", false)]
        [InlineData(ChargeItemCaseTypeOfService.OtherService, ChargeItemCase.ZeroVatRate, "Λοιπά τέλη", false)]
        /* Special tax item, allowed VAT code + mapped vatable fee */
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.NormalVatRate, "Λοιπά τέλη", true)]
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.DiscountedVatRate1, "Λοιπά τέλη", true)]
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.SuperReducedVatRate1, "Λοιπά τέλη", true)]
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.ZeroVatRate, "Λοιπά τέλη", true)]
        /* Special tax item, allowed VAT code but non-vatable fee */
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.NormalVatRate, "Εισφορά δακοκτονίας 2%", false)]
        /* Special tax item, allowed VAT code but unmapped fee */
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.NormalVatRate, "Unknown Fee", false)]
        /* Special tax item, allowed VAT code, but empty or null description */
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.NormalVatRate, "", false)]
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.NormalVatRate, null, false)]
        /* Special tax item, disallowed VAT code (should return false regardless of description) */
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.ParkingVatRate, "Λοιπά τέλη", false)]
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.NotTaxable, "Λοιπά τέλη", false)]
        [InlineData((ChargeItemCaseTypeOfService) 0xF0, ChargeItemCase.UnknownService, "Λοιπά τέλη", false)]
        public void IsVatableSpecialTaxItem_ShouldReturnExpectedResult(
            ChargeItemCaseTypeOfService serviceType,
            ChargeItemCase vatCode,
            string description,
            bool expected
        )
        {
            // If you use a WithTypeOfService extension, apply it; else just use the enum value directly.
            var chargeCase = vatCode.WithTypeOfService(serviceType);

            var chargeItem = new ChargeItem
            {
                Description = description,
                ftChargeItemCase = chargeCase
            };

            var result = SpecialTaxMappings.IsVatableSpecialTaxItem(chargeItem);

            result.Should().Be(expected, $"serviceType={serviceType}, vatCode={vatCode}, description='{description ?? "null"}'");
        }
        #endregion IsVatableSpecialTaxItemTests

        #region PunctuationCodepointVariants

        // Special tax descriptions are matched by string, so a description that differs only in the
        // codepoint of its punctuation used to be rejected. The AADE table uses the curly apostrophe
        // ("Περιπτ. δ’ - Τεχνικά Έργα - 3%"), while our table has the plain ASCII apostrophe. A
        // partner copying the description from the AADE spec could not be matched.

        [Theory]
        // Control: the exact ASCII form the table stores. Matches today and must keep matching.
        [InlineData("Περιπτ. δ' - Τεχνικά Έργα - 3%", 4)]
        // The AADE form. Same as the control except the apostrophe is the curly one.
        [InlineData("Περιπτ. δ’ - Τεχνικά Έργα - 3%", 4)]
        // The other three withholding categories whose official description carries an apostrophe.
        [InlineData("Περιπτ. β’- Τόκοι - 15%", 1)]
        [InlineData("Περιπτ. γ’ - Δικαιώματα - 20%", 2)]
        [InlineData("Περιπτ. δ’ - Αμοιβές Συμβουλών Διοίκησης - 20%", 3)]
        public void GetWithholdingTaxMapping_ShouldResolve_RegardlessOfApostropheCodepoint(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetWithholdingTaxMapping(description);

            mapping.Should().NotBeNull($"'{description}' is withholding category {expectedCode}; the apostrophe codepoint must not decide whether the category is found");
            mapping.Code.Should().Be(expectedCode, $"'{description}' must resolve to withholding category {expectedCode}");
        }

        // The same problem the other way round. A few entries in the other tax table are stored
        // with the en dash, so a partner sending the plain hyphen was rejected. This is why the
        // normalisation has to run on the table keys too, not just on the incoming description.

        [Theory]
        // Control: the exact en dash form the table stores. Matches today and must keep matching.
        [InlineData("Ενοικιαζόμενα επιπλωμένα δωμάτια – διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.)", 24)]
        // The same description typed with an ordinary hyphen.
        [InlineData("Ενοικιαζόμενα επιπλωμένα δωμάτια - διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.)", 24)]
        [InlineData("Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€", 27)]
        [InlineData("Αυτοεξυπηρετούμενα καταλύματα - τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€", 27)]
        public void GetOtherTaxMapping_ShouldResolve_RegardlessOfDashCodepoint(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetOtherTaxMapping(description);

            mapping.Should().NotBeNull($"'{description}' is other tax category {expectedCode}; the dash codepoint must not decide whether the category is found");
            mapping.Code.Should().Be(expectedCode, $"'{description}' must resolve to other tax category {expectedCode}");
        }

        // A description that is not in any table must still be rejected. Normalising punctuation
        // must not widen the match so far that an unknown description resolves to some category
        // through the substring fallback.
        [Theory]
        [InlineData("Unknown withholding tax")]
        [InlineData("Περιπτ. ζ’ - Ανύπαρκτη Κατηγορία - 7%")]
        public void GetWithholdingTaxMapping_ShouldStillReturnNull_ForDescriptionsNotInTheTable(string description)
        {
            SpecialTaxMappings.GetWithholdingTaxMapping(description).Should().BeNull($"'{description}' is not a real withholding category");
        }

        #region NormalizeForLookup

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData("\t \u00A0", "")]
        // Apostrophe variants all fold to the ASCII apostrophe U+0027.
        [InlineData("δ\u2019", "δ'")] // ’ right single quotation mark (AADE)
        [InlineData("δ\u2018", "δ'")] // ‘ left single quotation mark
        [InlineData("δ\u201B", "δ'")] // ‛ single high-reversed-9 quotation mark
        [InlineData("δ\u02BC", "δ'")] // ʼ modifier letter apostrophe
        [InlineData("δ\u02B9", "δ'")] // ʹ modifier letter prime
        [InlineData("δ\u0374", "δ'")] // ʹ Greek numeral sign (dexia keraia)
        [InlineData("δ\u0384", "δ'")] // ΄ Greek tonos
        [InlineData("δ\u00B4", "δ'")] // ´ acute accent
        [InlineData("δ\u2032", "δ'")] // ′ prime
        [InlineData("δ`", "δ'")]      // grave accent
        // Dash variants all fold to the ASCII hyphen-minus U+002D.
        [InlineData("α\u2010β", "α-β")] // ‐ hyphen
        [InlineData("α\u2011β", "α-β")] // ‑ non-breaking hyphen
        [InlineData("α\u2012β", "α-β")] // ‒ figure dash
        [InlineData("α\u2013β", "α-β")] // – en dash
        [InlineData("α\u2014β", "α-β")] // — em dash
        [InlineData("α\u2015β", "α-β")] // ― horizontal bar
        [InlineData("α\u2212β", "α-β")] // − minus sign
        // Whitespace: non-breaking and repeated whitespace collapses, ends are trimmed.
        [InlineData("α\u00A0β", "α β")]
        [InlineData("α  β", "α β")]
        [InlineData(" α \t β ", "α β")]
        // The issue 279 payload folds to the exact form the mapping table stores.
        [InlineData("Περιπτ. δ\u2019 - Τεχνικά Έργα - 3%", "Περιπτ. δ' - Τεχνικά Έργα - 3%")]
        // Already-normal input passes through unchanged.
        [InlineData("Περιπτ. δ' - Τεχνικά Έργα - 3%", "Περιπτ. δ' - Τεχνικά Έργα - 3%")]
        public void NormalizeForLookup_ShouldFoldPunctuationAndWhitespace(string? input, string expected)
        {
            SpecialTaxMappings.NormalizeForLookup(input).Should().Be(expected);
        }

        #endregion NormalizeForLookup

        #region FullTableVariantCoverage

        // Every single description of all four AADE tables, in the exact form the mapping table
        // stores it. These catalogs intentionally duplicate the production tables: a silent change
        // to a description or code in SpecialTaxMappings must fail here.
        public static readonly (string Description, int Code)[] AllWithholdingTaxDescriptions =
        [
            ("Περιπτ. β'- Τόκοι - 15%", 1),
            ("Περιπτ. γ' - Δικαιώματα - 20%", 2),
            ("Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%", 3),
            ("Περιπτ. δ' - Τεχνικά Έργα - 3%", 4),
            ("Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%", 5),
            ("Λοιπά Αγαθά 4%", 6),
            ("Παροχή Υπηρεσιών 8%", 7),
            ("Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%", 8),
            ("Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%", 9),
            ("Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%", 10),
            ("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013", 11),
            ("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού", 12),
            ("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού", 13),
            ("Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", 14),
            ("Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013", 15),
            ("Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)", 16),
            ("Λοιπές Παρακρατήσεις Φόρου", 17),
            ("Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013", 18),
        ];

        public static readonly (string Description, int Code)[] AllFeeDescriptions =
        [
            ("Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%", 1),
            ("Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%", 2),
            ("Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%", 3),
            ("Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%", 4),
            ("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)", 5),
            ("Τέλος στη συνδρομητική τηλεόραση 10%", 6),
            ("Τέλος συνδρομητών σταθερής τηλεφωνίας 5%", 7),
            ("Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", 8),
            ("Εισφορά δακοκτονίας 2%", 9),
            ("Λοιπά τέλη", 10),
            ("Τέλη Λοιπών Φόρων", 11),
            ("Εισφορά δακοκτονίας", 12),
            ("Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)", 13),
            ("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)", 14),
            ("Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)", 15),
            ("Εισφορά προστασίας περιβάλλοντος πλαστικών προϊόντων 0,04 λεπτά ανά τεμάχιο [άρθρο 4 ν. 4736/2020]", 16),
            ("Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο [άρθρο 80 ν. 4819/2021]", 17),
            ("Τέλος διαμονής παρεπιδημούντων", 18),
            ("Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων", 19),
            ("Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης", 20),
            ("Τέλος επί των ακαθάριστων εσόδων των καζίνο", 21),
            ("Λοιπά τέλη επί των ακαθάριστων εσόδων", 22),
        ];

        public static readonly (string Description, int Code)[] AllStampDutyDescriptions =
        [
            ("Συντελεστής 1,2 %", 1),
            ("Συντελεστής 2,4 %", 2),
            ("Συντελεστής 3,6 %", 3),
            ("Λοιπές περιπτώσεις Χαρτοσήμου", 4),
        ];

        public static readonly (string Description, int Code)[] AllOtherTaxDescriptions =
        [
            ("α1) ασφάλιστρα κλάδου πυρός 20%", 1),
            ("α2) ασφάλιστρα κλάδου πυρός 20%", 2),
            ("β) ασφάλιστρα κλάδου ζωής 4%", 3),
            ("γ) ασφάλιστρα λοιπών κλάδων 15%", 4),
            ("δ) απαλλασσόμενα φόρου ασφαλίστρων 0%", 5),
            ("Ξενοδοχεία 1-2 αστέρων 0,50 €", 6),
            ("Ξενοδοχεία 3 αστέρων 1,50 €", 7),
            ("Ξενοδοχεία 4 αστέρων 3,00 €", 8),
            ("Ξενοδοχεία 4 αστέρων 4,00 €", 9), // official AADE wording (Appendix 8.5), despite the apparent "4 αστέρων" typo
            ("Ξενοδοχεία 5 αστέρων 4,00 €", 9), // alias: the logically correct 5-star wording
            ("Ενοικιαζόμενα - επιπλωμένα δωμάτια - διαμερίσματα 0,50 €", 10),
            ("Ειδικός Φόρος στις διαφημίσεις που προβάλλονται από την τηλεόραση (ΕΦΤΔ) 5%", 11),
            ("3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτούμενα και εισαγόμενα από τρίτες χώρες 10%", 12),
            ("3.2 Φόρος πολυτελείας 10% επί της τιμής πώλησης προ Φ.Π.Α. για τα εγχωρίως παραγόμενα είδη 10%", 13),
            ("Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)", 14),
            ("ασφάλιστρα κλάδου πυρός 20%", 15),
            ("Λοιποί Τελωνειακοί Δασμοί-Φόροι", 16),
            ("Λοιποί Φόροι", 17),
            ("Επιβαρύνσεις Λοιπών Φόρων", 18),
            ("ΕΦΚ", 19),
            ("Ξενοδοχεία 1-2 αστέρων 1,50€ (ανά Δωμ./Διαμ.)", 20),
            ("Ξενοδοχεία 3 αστέρων 3,00€ (ανά Δωμ./Διαμ.)", 21),
            ("Ξενοδοχεία 4 αστέρων 7,00€ (ανά Δωμ./Διαμ.)", 22),
            ("Ξενοδοχεία 5 αστέρων 10,00€ (ανά Δωμ./Διαμ.)", 23),
            ("Ενοικιαζόμενα επιπλωμένα δωμάτια – διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.)", 24),
            ("Ακίνητα βραχυχρόνιας μίσθωσης 1,50€", 25),
            ("Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 10,00€", 26),
            ("Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€", 27),
            ("Ακίνητα βραχυχρόνιας μίσθωσης 0,50€", 28),
            ("Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 4,00€", 29),
            ("Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 4,00€", 30),
        ];

        /// <summary>
        /// Produces the punctuation, whitespace, and casing variants a real POS or partner system
        /// might send for a stored description. The first variant is the description itself.
        /// </summary>
        private static IEnumerable<string> PunctuationVariants(string description)
        {
            yield return description;                            // exactly as stored
            yield return description.Replace('\'', '\u2019');    // ’ the form AADE publishes (issue 279)
            yield return description.Replace('\'', '\u2018');    // ‘ left single quotation mark
            yield return description.Replace('\'', '\u0384');    // ΄ Greek tonos, produced by Greek keyboards
            yield return description.Replace('\'', '\u0374');    // ʹ Greek numeral sign, the typographically correct mark
            yield return description.Replace('\'', '\u00B4');    // ´ acute accent
            yield return description.Replace('\'', '`');         // grave accent
            yield return description.Replace('-', '\u2013');     // every hyphen as an en dash
            yield return description.Replace('-', '\u2014');     // every hyphen as an em dash
            yield return description.Replace('\u2013', '-');     // stored en dashes typed as plain hyphens
            yield return description.Replace(' ', '\u00A0');     // non-breaking spaces
            yield return description.Replace(" ", "  ");         // doubled internal whitespace
            yield return "  " + description + " \t";             // stray surrounding whitespace
            yield return description.ToUpperInvariant();         // case-insensitivity
            yield return description.ToLowerInvariant();
        }

        private static IEnumerable<object[]> GenerateVariantCases((string Description, int Code)[] entries)
        {
            foreach (var (description, code) in entries)
            {
                foreach (var variant in PunctuationVariants(description).Distinct())
                {
                    yield return new object[] { variant, code };
                }
            }
        }

        public static IEnumerable<object[]> WithholdingTaxVariantCases => GenerateVariantCases(AllWithholdingTaxDescriptions);
        public static IEnumerable<object[]> FeeVariantCases => GenerateVariantCases(AllFeeDescriptions);
        public static IEnumerable<object[]> StampDutyVariantCases => GenerateVariantCases(AllStampDutyDescriptions);
        public static IEnumerable<object[]> OtherTaxVariantCases => GenerateVariantCases(AllOtherTaxDescriptions);

        [Theory]
        [MemberData(nameof(WithholdingTaxVariantCases))]
        public void GetWithholdingTaxMapping_ShouldResolveEveryDescription_InAllPunctuationVariants(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetWithholdingTaxMapping(description);

            mapping.Should().NotBeNull($"'{description}' is withholding category {expectedCode}; punctuation, whitespace, or casing must not decide whether the category is found");
            mapping.Code.Should().Be(expectedCode, $"'{description}' must resolve to withholding category {expectedCode}");
        }

        [Theory]
        [MemberData(nameof(FeeVariantCases))]
        public void GetFeeMapping_ShouldResolveEveryDescription_InAllPunctuationVariants(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetFeeMapping(description);

            mapping.Should().NotBeNull($"'{description}' is fee category {expectedCode}; punctuation, whitespace, or casing must not decide whether the category is found");
            mapping.Code.Should().Be(expectedCode, $"'{description}' must resolve to fee category {expectedCode}");
        }

        [Theory]
        [MemberData(nameof(StampDutyVariantCases))]
        public void GetStampDutyMapping_ShouldResolveEveryDescription_InAllPunctuationVariants(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetStampDutyMapping(description);

            mapping.Should().NotBeNull($"'{description}' is stamp duty category {expectedCode}; punctuation, whitespace, or casing must not decide whether the category is found");
            mapping.Code.Should().Be(expectedCode, $"'{description}' must resolve to stamp duty category {expectedCode}");
        }

        [Theory]
        [MemberData(nameof(OtherTaxVariantCases))]
        public void GetOtherTaxMapping_ShouldResolveEveryDescription_InAllPunctuationVariants(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetOtherTaxMapping(description);

            mapping.Should().NotBeNull($"'{description}' is other tax category {expectedCode}; punctuation, whitespace, or casing must not decide whether the category is found");
            mapping.Code.Should().Be(expectedCode, $"'{description}' must resolve to other tax category {expectedCode}");
        }

        // Normalisation must never collapse two different table entries into the same lookup key,
        // otherwise one of them would silently become unreachable.
        [Fact]
        public void NormalizeForLookup_ShouldKeepEveryTableEntryDistinct()
        {
            foreach (var catalog in new[] { AllWithholdingTaxDescriptions, AllFeeDescriptions, AllStampDutyDescriptions, AllOtherTaxDescriptions })
            {
                var normalizedKeys = catalog
                    .Select(entry => SpecialTaxMappings.NormalizeForLookup(entry.Description).ToUpperInvariant())
                    .ToList();

                normalizedKeys.Should().OnlyHaveUniqueItems("normalising descriptions must not merge distinct tax categories");
            }
        }

        #endregion FullTableVariantCoverage

        #region ExactMatchPrecedence

        // Several table entries are substrings of one another ("Εισφορά δακοκτονίας" is contained
        // in "Εισφορά δακοκτονίας 2%", "Λοιπά τέλη" in "Λοιπά τέλη επί των ακαθάριστων εσόδων",
        // "ασφάλιστρα κλάδου πυρός 20%" in "α1) ασφάλιστρα κλάδου πυρός 20%"). The exact-match pass
        // must win before the substring fallback gets a chance to return the wrong category.
        [Theory]
        [InlineData("Εισφορά δακοκτονίας", 12)]
        [InlineData("Εισφορά δακοκτονίας 2%", 9)]
        [InlineData("Λοιπά τέλη", 10)]
        [InlineData("Λοιπά τέλη επί των ακαθάριστων εσόδων", 22)]
        public void GetFeeMapping_ShouldPreferExactMatch_OverSubstringFallback(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetFeeMapping(description);

            mapping.Should().NotBeNull();
            mapping.Code.Should().Be(expectedCode, $"the exact entry '{description}' exists and must win over any substring match");
        }

        [Theory]
        [InlineData("ασφάλιστρα κλάδου πυρός 20%", 15)]
        [InlineData("α1) ασφάλιστρα κλάδου πυρός 20%", 1)]
        [InlineData("α2) ασφάλιστρα κλάδου πυρός 20%", 2)]
        public void GetOtherTaxMapping_ShouldPreferExactMatch_OverSubstringFallback(string description, int expectedCode)
        {
            var mapping = SpecialTaxMappings.GetOtherTaxMapping(description);

            mapping.Should().NotBeNull();
            mapping.Code.Should().Be(expectedCode, $"the exact entry '{description}' exists and must win over any substring match");
        }

        #endregion ExactMatchPrecedence

        #region NullEmptyAndUnknownInput

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\u00A0\t")]
        public void AllLookups_ShouldReturnNull_ForNullOrWhitespaceDescriptions(string? description)
        {
            SpecialTaxMappings.GetWithholdingTaxMapping(description!).Should().BeNull();
            SpecialTaxMappings.GetFeeMapping(description!).Should().BeNull();
            SpecialTaxMappings.GetStampDutyMapping(description!).Should().BeNull();
            SpecialTaxMappings.GetOtherTaxMapping(description!).Should().BeNull();
        }

        [Theory]
        [InlineData("Some random product description")]
        [InlineData("Φόρος που δεν υπάρχει 99%")]
        [InlineData("’’’’")]
        [InlineData("---")]
        public void AllLookups_ShouldReturnNull_ForUnknownDescriptions_EvenWithSpecialCharacters(string description)
        {
            SpecialTaxMappings.GetWithholdingTaxMapping(description).Should().BeNull();
            SpecialTaxMappings.GetFeeMapping(description).Should().BeNull();
            SpecialTaxMappings.GetStampDutyMapping(description).Should().BeNull();
            SpecialTaxMappings.GetOtherTaxMapping(description).Should().BeNull();
        }

        #endregion NullEmptyAndUnknownInput

        #endregion PunctuationCodepointVariants
    }
}