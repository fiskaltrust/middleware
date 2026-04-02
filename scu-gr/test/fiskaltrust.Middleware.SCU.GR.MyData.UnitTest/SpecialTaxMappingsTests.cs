using System;
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
        [InlineData("Ξενοδοχεία 4 αστέρων 4,00 €", 9, null, true)]
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
    }
}